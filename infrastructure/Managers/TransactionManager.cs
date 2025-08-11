using application.Commands.Transactions;
using application.Contracts.Managers;
using application.Contracts.Repos;
using application.Contracts.Response;
using application.Contracts.Services;
using application.Queries.Transactions;
using domain.Enums;
using domain.Response;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace infrastructure.Managers;

public class TransactionManager(
    IUnitOfWork unitOfWork,
    IPaymentService paymentService,
    ICacheService cacheService,
    IBackgroundJobClient backgroundJobClient,
    ILogger<TransactionManager> logger,
    IEncryptionService encryptionService) : ITransactionManager
{
    public async Task<ConfirmDepositResult> ConfirmDeposit(string paymentReference, string paystackReference, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Confirming deposit for payment reference: {PaymentReference}", paymentReference);

        try
        {
            // Get cached deposit data
            var depositData = await cacheService.GetData<DepositCacheData>(
                $"deposit_pending:{paymentReference}");

            if (depositData == null)
            {
                throw new InvalidOperationException("Deposit request not found or has expired.");
            }

            // Verify payment with Paystack
            var verification = paymentService.CheckTransactionByRefQuery(paystackReference);

            if (verification?.Status != true || verification.Data?.Status?.ToLower() != "success")
            {
                return new ConfirmDepositResult(
                    Guid.Empty,
                    depositData.AccountId,
                    depositData.Amount,
                    0,
                    depositData.Description,
                    paymentReference,
                    DateTime.UtcNow,
                    false,
                    $"Payment verification failed: {verification?.Message ?? "Payment not successful"}"
                );
            }

            // Get account
            var account = await unitOfWork.Accounts.GetByIdAsync(depositData.AccountId, cancellationToken);
            if (account == null)
            {
                throw new InvalidOperationException("Account not found.");
            }

            await unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                // Process the deposit
                var previousBalance = account.Balance.Amount;
                var trx = account.Deposit(depositData.Amount, $"{depositData.Description} - Paystack Ref: {paystackReference}");

                await unitOfWork.Transactions.AddAsync(trx, cancellationToken);
                await unitOfWork.Accounts.UpdateAsync(account, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                await unitOfWork.CommitTransactionAsync(cancellationToken);

                // Remove from pending cache
                await cacheService.RemoveData($"deposit_pending:{paymentReference}");

                // Cache successful transaction for reference
                await cacheService.SetDataAsync(
                    $"deposit_completed:{paymentReference}",
                    new { AccountId = account.Id, Amount = depositData.Amount, CompletedAt = DateTime.UtcNow },
                    TimeSpan.FromDays(30));

                var transaction = account.Transactions.OrderByDescending(t => t.CreatedAt).First();

                logger.LogInformation("Deposit confirmed successfully. Transaction ID: {TransactionId}", transaction.Id);

                return new ConfirmDepositResult(
                    transaction.Id,
                    account.Id,
                    transaction.Amount.Amount,
                    account.Balance.Amount,
                    transaction.Description,
                    paymentReference,
                    transaction.CreatedAt,
                    true,
                    "Deposit completed successfully"
                );
            }
            catch
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error confirming deposit for payment reference: {PaymentReference}", paymentReference);
            throw;
        }
    }

    public async Task<ConfirmWithdrawalResult> ConfirmWithdrawal(string transferReference, string paystackTransferCode, bool isSuccessful, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Confirming withdrawal for transfer reference: {TransferReference}, Success: {IsSuccessful}",
            transferReference, isSuccessful);

        try
        {
            // Get cached withdrawal data
            var withdrawalData = await cacheService.GetData<WithdrawalCacheData>(
                $"withdrawal_pending:{transferReference}");

            if (withdrawalData == null)
            {
                throw new InvalidOperationException("Withdrawal request not found or has expired.");
            }

            if (isSuccessful)
            {
                // Transfer was successful - update transaction status
                await cacheService.RemoveData($"withdrawal_pending:{transferReference}");

                // Cache successful transfer for reference
                await cacheService.SetDataAsync(
                    $"withdrawal_completed:{transferReference}",
                    new
                    {
                        AccountId = withdrawalData.AccountId,
                        Amount = withdrawalData.Amount,
                        CompletedAt = DateTime.UtcNow,
                        PaystackTransferCode = paystackTransferCode
                    },
                    TimeSpan.FromDays(30));

                // Get account to return current balance
                var account = await unitOfWork.Accounts.GetByIdAsync(withdrawalData.AccountId, cancellationToken);
                var transaction = account?.Transactions.OrderByDescending(t => t.CreatedAt).FirstOrDefault();

                logger.LogInformation("Withdrawal confirmed successfully. Transfer Reference: {TransferReference}", transferReference);

                return new ConfirmWithdrawalResult(
                    transaction?.Id ?? Guid.Empty,
                    withdrawalData.AccountId,
                    withdrawalData.Amount,
                    account?.Balance.Amount ?? 0,
                    withdrawalData.Description,
                    transferReference,
                    DateTime.UtcNow,
                    true,
                    "Transfer completed successfully.",
                    TransferStatus.Completed
                );
            }
            else
            {
                // Transfer failed - need to reverse the withdrawal
                return await ReverseWithdrawal(transferReference, paystackTransferCode, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error confirming withdrawal for transfer reference: {TransferReference}", transferReference);
            throw;
        }
    }

    public async Task<DepositResult> DirectDeposit(DepositCommand request, CancellationToken cancellationToken)
    {
        var account = await unitOfWork.Accounts.GetByIdAsync(request.AccountId, cancellationToken);
        if (account == null)
        {
            throw new InvalidOperationException($"Account with ID '{request.AccountId}' not found.");
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var trx = account.Deposit(request.Amount, request.Description);

            await unitOfWork.Transactions.AddAsync(trx, cancellationToken);
            await unitOfWork.Accounts.UpdateAsync(account, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            var transaction = account.Transactions.OrderByDescending(t => t.CreatedAt).First();

            return new DepositResult(
                transaction.Id,
                account.Id,
                transaction.Amount.Amount,
                account.Balance.Amount,
                transaction.Description,
                transaction.CreatedAt);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<GetBanksResult> GetBanks(GetBanksQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var banks = await paymentService.GetAllBanks();
            return new GetBanksResult(banks, true, "Banks retrieved successfully");
        }
        catch (Exception ex)
        {
            return new GetBanksResult(
                Enumerable.Empty<BankInfo>(),
                false,
                $"Failed to retrieve banks: {ex.Message}");
        }
    }

    public async Task<MonthlyStatementDto> GetMonthlyStatement(GetMonthlyStatementQuery request, CancellationToken cancellationToken)
    {
        var account = await unitOfWork.Accounts.GetByIdAsync(request.AccountId, cancellationToken);
        if (account == null)
        {
            throw new InvalidOperationException($"Account with ID '{request.AccountId}' not found.");
        }

        var monthlyTransactions = account.GetMonthlyStatement(request.Year, request.Month).ToList();

        var deposits = monthlyTransactions
            .Where(t => t.Type == TransactionType.Deposit || t.Type == TransactionType.TransferReceived)
            .Sum(t => t.Amount.Amount);

        var withdrawals = monthlyTransactions
            .Where(t => t.Type == TransactionType.Withdrawal || t.Type == TransactionType.Transfer)
            .Sum(t => t.Amount.Amount);

        // Calculate opening balance (current balance - net change during month)
        var netChange = deposits - withdrawals;
        var openingBalance = account.Balance.Amount - netChange;

        var transactionDtos = monthlyTransactions
            .Select(t => new TransactionDto(
                t.Id,
                t.Type,
                t.Amount.Amount,
                t.Amount.Currency,
                t.Description,
                t.CreatedAt,
                t.Status,
                t.Reference ?? string.Empty,
                t.RelatedAccountId))
            .ToList();

        return new MonthlyStatementDto(
            account.Id,
            account.AccountNumber,
            request.Year,
            request.Month,
            openingBalance,
            account.Balance.Amount,
            deposits,
            withdrawals,
            monthlyTransactions.Count,
            transactionDtos);
    }

    public async Task<TransactionHistoryDto> GetTransactionHistory(GetTransactionHistoryQuery request, CancellationToken cancellationToken)
    {
        var account = await unitOfWork.Accounts.GetByIdAsync(request.AccountId, cancellationToken);
        if (account == null)
        {
            throw new InvalidOperationException($"Account with ID '{request.AccountId}' not found.");
        }

        var transactions = account.GetTransactionHistory(request.FromDate, request.ToDate);
        var totalCount = transactions.Count();

        var pagedTransactions = transactions
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new TransactionDto(
                t.Id,
                t.Type,
                t.Amount.Amount,
                t.Amount.Currency,
                t.Description,
                t.CreatedAt,
                t.Status,
                t.Reference ?? string.Empty,
                t.RelatedAccountId))
            .ToList();

        return new TransactionHistoryDto(
            request.AccountId,
            pagedTransactions,
            totalCount,
            request.PageNumber,
            request.PageSize);
    }

    public async Task<InitiateDepositResult> HandleDeposit(InitiateDepositCommand request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Initiating Paystack deposit for account {AccountId}, amount: {Amount}",
        request.AccountId, request.Amount);

        // Get account and validate
        var account = await unitOfWork.Accounts.GetByIdAsync(request.AccountId, cancellationToken);
        if (account == null)
        {
            throw new InvalidOperationException($"Account with ID '{request.AccountId}' not found.");
        }

        if (!account.IsActive)
        {
            throw new InvalidOperationException("Account is not active.");
        }

        // Get user email if not provided
        var user = await unitOfWork.Users.GetByIdAsync(account.UserId, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException("Account owner not found.");
        }

        var customerEmail = request.CustomerEmail ?? user.Email;

        // Generate unique payment reference
        var paymentReference = $"DEP_{DateTime.UtcNow:yyyyMMddHHmmss}_{account.Id.ToString()[..8]}";

        try
        {
            // Initialize Paystack payment
            var paystackResponse = paymentService.MakePaystackDeposit(
                customerEmail,
                paymentReference,
                (int)request.Amount);

            if (paystackResponse?.Status != true)
            {
                throw new InvalidOperationException($"Failed to initialize Paystack payment: {paystackResponse?.Message}");
            }

            // Cache the deposit request for later confirmation
            var depositCacheData = new DepositCacheData
            {
                AccountId = request.AccountId,
                Amount = request.Amount,
                Description = request.Description,
                CustomerEmail = customerEmail,
                PaymentReference = paymentReference,
                PaystackReference = paystackResponse.Data.Reference,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(1) // Payment expires in 1 hour
            };

            await cacheService.SetDataAsync(
                $"deposit_pending:{paymentReference}",
                depositCacheData,
                TimeSpan.FromHours(2));

            logger.LogInformation("Paystack deposit initiated successfully. Reference: {PaymentReference}", paymentReference);

            // Replace with a static method approach:
            backgroundJobClient.Schedule<ITransactionManager>(
                x => x.ConfirmDeposit(paymentReference, paystackResponse.Data.Reference, default),
                TimeSpan.FromSeconds(30));

            return new InitiateDepositResult(
                paymentReference,
                paystackResponse.Data.AuthorizationUrl,
                paystackResponse.Data.AccessCode,
                request.Amount,
                "NGN",
                depositCacheData.ExpiresAt,
                request.AccountId,
                "Payment link generated successfully. Complete payment to credit your account."
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error initiating Paystack deposit for account {AccountId}", request.AccountId);
            throw;
        }
    }

    public async Task<InitiateWithdrawalResult> InitiateExternalWithdrawal(InitiateWithdrawalCommand request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Initiating external withdrawal for account {AccountId}, amount: {Amount} to {BankCode}-{AccountNumber}",
            request.AccountId, request.Amount, request.BankCode, request.AccountNumber);

        try
        {
            // Get and validate account
            var account = await unitOfWork.Accounts.GetByIdAsync(request.AccountId, cancellationToken);
            if (account == null)
            {
                return new InitiateWithdrawalResult(
                    string.Empty,
                    request.AccountId,
                    request.Amount,
                    request.BankCode,
                    request.AccountNumber,
                    request.AccountName,
                    DateTime.UtcNow,
                    false,
                    "Account not found."
                );
            }

            // Validate PIN
            if (!encryptionService.VerifyPIN(request.PIN, account.PINHash))
            {
                return new InitiateWithdrawalResult(
                    string.Empty,
                    request.AccountId,
                    request.Amount,
                    request.BankCode,
                    request.AccountNumber,
                    request.AccountName,
                    DateTime.UtcNow,
                    false,
                    "Invalid PIN provided."
                );
            }

            if (!account.IsActive)
            {
                return new InitiateWithdrawalResult(
                    string.Empty,
                    request.AccountId,
                    request.Amount,
                    request.BankCode,
                    request.AccountNumber,
                    request.AccountName,
                    DateTime.UtcNow,
                    false,
                    "Account is not active."
                );
            }

            if (account.Balance.Amount < request.Amount)
            {
                return new InitiateWithdrawalResult(
                    string.Empty,
                    request.AccountId,
                    request.Amount,
                    request.BankCode,
                    request.AccountNumber,
                    request.AccountName,
                    DateTime.UtcNow,
                    false,
                    "Insufficient funds."
                );
            }

            // Verify account details with Paystack
            var accountValidation = await paymentService.GetAccountName(request.AccountNumber, request.BankCode);
            if (accountValidation == null || string.IsNullOrEmpty(accountValidation.account_name))
            {
                return new InitiateWithdrawalResult(
                    string.Empty,
                    request.AccountId,
                    request.Amount,
                    request.BankCode,
                    request.AccountNumber,
                    request.AccountName,
                    DateTime.UtcNow,
                    false,
                    "Unable to verify account details. Please check account number and bank code."
                );
            }

            // Generate transfer reference
            var transferReference = $"WTH_{DateTime.UtcNow:yyyyMMddHHmmss}_{account.Id.ToString()[..8]}";

            // Create Paystack recipient
            var recipientResponse = await paymentService.CreateRecipient(
                request.AccountName,
                request.AccountNumber,
                request.BankCode
            );

            if (!recipientResponse.status)
            {
                return new InitiateWithdrawalResult(
                    transferReference,
                    request.AccountId,
                    request.Amount,
                    request.BankCode,
                    request.AccountNumber,
                    request.AccountName,
                    DateTime.UtcNow,
                    false,
                    $"Failed to create transfer recipient: {recipientResponse.message}"
                );
            }

            // Initiate transfer
            var transferResponse = await paymentService.InitiateTransferHttp(
                (int)(request.Amount * 100), // Convert to kobo
                recipientResponse.data.recipient_code,
                request.Description
            );

            if (!transferResponse.status)
            {
                return new InitiateWithdrawalResult(
                    transferReference,
                    request.AccountId,
                    request.Amount,
                    request.BankCode,
                    request.AccountNumber,
                    request.AccountName,
                    DateTime.UtcNow,
                    false,
                    $"Failed to initiate transfer: {transferResponse.message}"
                );
            }

            // Begin transaction to withdraw from account
            await unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                // Create pending withdrawal transaction
                account.Withdraw(request.Amount, $"{request.Description} - Pending Transfer");

                await unitOfWork.Accounts.UpdateAsync(account, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                await unitOfWork.CommitTransactionAsync(cancellationToken);

                // Cache withdrawal data for webhook processing
                var withdrawalCacheData = new WithdrawalCacheData
                {
                    AccountId = request.AccountId,
                    Amount = request.Amount,
                    Description = request.Description,
                    BankCode = request.BankCode,
                    AccountNumber = request.AccountNumber,
                    AccountName = request.AccountName,
                    TransferReference = transferReference,
                    PaystackRecipientCode = recipientResponse.data.recipient_code,
                    PaystackTransferCode = transferResponse.data.transfer_code,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddHours(24)
                };

                await cacheService.SetDataAsync(
                    $"withdrawal_pending:{transferReference}",
                    withdrawalCacheData,
                    TimeSpan.FromDays(2)
                );

                logger.LogInformation("External withdrawal initiated successfully. Transfer Reference: {TransferReference}", transferReference);

                return new InitiateWithdrawalResult(
                    transferReference,
                    request.AccountId,
                    request.Amount,
                    request.BankCode,
                    request.AccountNumber,
                    request.AccountName,
                    DateTime.UtcNow,
                    true,
                    "Transfer initiated successfully. Awaiting confirmation from Paystack.",
                    recipientResponse.data.recipient_code,
                    transferResponse.data.transfer_code
                );
            }
            catch
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error initiating external withdrawal for account {AccountId}", request.AccountId);
            throw;
        }
    }

    public async Task<ConfirmWithdrawalResult> ReverseWithdrawal(string transferReference, string paystackTransferCode, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Reversing withdrawal for transfer reference: {TransferReference}", transferReference);

        try
        {
            // Get cached withdrawal data
            var withdrawalData = await cacheService.GetData<WithdrawalCacheData>(
                $"withdrawal_pending:{transferReference}");

            if (withdrawalData == null)
            {
                throw new InvalidOperationException("Withdrawal request not found or has expired.");
            }

            // Get account and reverse the withdrawal
            var account = await unitOfWork.Accounts.GetByIdAsync(withdrawalData.AccountId, cancellationToken);
            if (account == null)
            {
                throw new InvalidOperationException("Account not found.");
            }

            await unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                // Deposit the amount back (reverse the withdrawal)
                account.Deposit(withdrawalData.Amount, $"Reversal: {withdrawalData.Description} - Transfer Failed");

                await unitOfWork.Accounts.UpdateAsync(account, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                await unitOfWork.CommitTransactionAsync(cancellationToken);

                // Remove from pending and cache as reversed
                await cacheService.RemoveData($"withdrawal_pending:{transferReference}");
                await cacheService.SetDataAsync(
                    $"withdrawal_reversed:{transferReference}",
                    new
                    {
                        AccountId = withdrawalData.AccountId,
                        Amount = withdrawalData.Amount,
                        ReversedAt = DateTime.UtcNow,
                        PaystackTransferCode = paystackTransferCode
                    },
                    TimeSpan.FromDays(30));

                var reversalTransaction = account.Transactions.OrderByDescending(t => t.CreatedAt).First();

                logger.LogInformation("Withdrawal reversed successfully. Transfer Reference: {TransferReference}", transferReference);

                return new ConfirmWithdrawalResult(
                    reversalTransaction.Id,
                    account.Id,
                    withdrawalData.Amount,
                    account.Balance.Amount,
                    $"Reversal: {withdrawalData.Description}",
                    transferReference,
                    DateTime.UtcNow,
                    false,
                    "Transfer failed and amount has been refunded to your account.",
                    TransferStatus.Reversed
                );
            }
            catch
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reversing withdrawal for transfer reference: {TransferReference}", transferReference);
            throw;
        }
    }

    public async Task<TransferResult> Transfer(TransferCommand request, CancellationToken cancellationToken)
    {
        var fromAccount = await unitOfWork.Accounts.GetByIdAsync(request.FromAccountId, cancellationToken);
        if (fromAccount == null)
        {
            throw new InvalidOperationException($"Source account with ID '{request.FromAccountId}' not found.");
        }

        var toAccount = await unitOfWork.Accounts.GetByIdAsync(request.ToAccountId, cancellationToken);
        if (toAccount == null)
        {
            throw new InvalidOperationException($"Destination account with ID '{request.ToAccountId}' not found.");
        }

        if (!encryptionService.VerifyPIN(request.PIN, fromAccount.PINHash))
        {
            throw new UnauthorizedAccessException("Invalid PIN provided.");
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var trxFrom = fromAccount.Transfer(request.ToAccountId, request.Amount, request.Description);
            var trxTo = toAccount.ReceiveTransfer(request.Amount, request.FromAccountId, request.Description);

            await unitOfWork.Transactions.AddAsync(trxFrom, cancellationToken);
            await unitOfWork.Transactions.AddAsync(trxTo, cancellationToken);
            await unitOfWork.Accounts.UpdateAsync(fromAccount, cancellationToken);
            await unitOfWork.Accounts.UpdateAsync(toAccount, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            var fromTransaction = fromAccount.Transactions.OrderByDescending(t => t.CreatedAt).First();
            var toTransaction = toAccount.Transactions.OrderByDescending(t => t.CreatedAt).First();

            return new TransferResult(
                fromTransaction.Id,
                toTransaction.Id,
                fromAccount.Id,
                toAccount.Id,
                request.Amount,
                fromAccount.Balance.Amount,
                toAccount.Balance.Amount,
                request.Description,
                fromTransaction.CreatedAt);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<VerifyAccountResult> VerifyAccount(VerifyAccountQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var accountInfo = await paymentService.GetAccountName(request.AccountNumber, request.BankCode);

            if (accountInfo != null && !string.IsNullOrEmpty(accountInfo.account_name))
            {
                return new VerifyAccountResult(
                    accountInfo.account_number,
                    accountInfo.account_name,
                    request.BankCode,
                    true,
                    "Account verification successful"
                );
            }
            else
            {
                return new VerifyAccountResult(
                    request.AccountNumber,
                    string.Empty,
                    request.BankCode,
                    false,
                    "Account not found or invalid details provided"
                );
            }
        }
        catch (Exception ex)
        {
            return new VerifyAccountResult(
                request.AccountNumber,
                string.Empty,
                request.BankCode,
                false,
                $"Error verifying account: {ex.Message}"
            );
        }
    }

    public async Task<WithdrawResult> Withdraw(WithdrawCommand request, CancellationToken cancellationToken)
    {
        var account = await unitOfWork.Accounts.GetByIdAsync(request.AccountId, cancellationToken);
        if (account == null)
        {
            return new WithdrawResult(
                Guid.Empty,
                request.AccountId,
                request.Amount,
                0,
                request.Description,
                DateTime.UtcNow,
                false,
                $"Account with ID '{request.AccountId}' not found."
            );
        }
        if (!encryptionService.VerifyPIN(request.PIN, account.PINHash))
        {
            return new WithdrawResult(
                Guid.Empty,
                request.AccountId,
                request.Amount,
                account.Balance.Amount,
                request.Description,
                DateTime.UtcNow,
                false,
                "Invalid PIN provided."
            );
        }
        if (!account.IsActive)
        {
            return new WithdrawResult(
                Guid.Empty,
                request.AccountId,
                request.Amount,
                account.Balance.Amount,
                request.Description,
                DateTime.UtcNow,
                false,
                "Account is not active."
            );
        }
        if (account.Balance.Amount < request.Amount)
        {
            return new WithdrawResult(
                Guid.Empty,
                request.AccountId,
                request.Amount,
                account.Balance.Amount,
                request.Description,
                DateTime.UtcNow,
                false,
                "Insufficient funds."
            );
        }
        // Handle internal transfers
        if (request.TransferType == TransferType.Internal)
        {
            return await HandleInternalTransfer(request, account, cancellationToken);
        }
        // For external transfers, we need additional processing via TransactionManager
        // This handler only handles simple internal withdrawals and transfers
        return new WithdrawResult(
            Guid.Empty,
            request.AccountId,
            request.Amount,
            account.Balance.Amount,
            request.Description,
            DateTime.UtcNow,
            false,
            "External transfers require additional processing. Use InitiateWithdrawal endpoint."
        );
    }

    private async Task<WithdrawResult> HandleInternalTransfer(WithdrawCommand request, domain.Entities.Account account, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            if (request.TargetAccountId.HasValue)
            {
                // Internal transfer to another account
                var targetAccount = await unitOfWork.Accounts.GetByIdAsync(request.TargetAccountId.Value, cancellationToken);
                if (targetAccount == null)
                {
                    return new WithdrawResult(
                        Guid.Empty,
                        request.AccountId,
                        request.Amount,
                        account.Balance.Amount,
                        request.Description,
                        DateTime.UtcNow,
                        false,
                        "Target account not found."
                    );
                }

                // Perform transfer
                var trxFrom = account.Transfer(request.TargetAccountId.Value, request.Amount, request.Description);
                var trxTo = targetAccount.ReceiveTransfer(request.Amount, request.AccountId, $"Transfer from {account.AccountNumber}");

                await unitOfWork.Transactions.AddAsync(trxFrom, cancellationToken);
                await unitOfWork.Transactions.AddAsync(trxTo, cancellationToken);
                await unitOfWork.Accounts.UpdateAsync(account, cancellationToken);
                await unitOfWork.Accounts.UpdateAsync(targetAccount, cancellationToken);
            }
            else
            {
                // Simple withdrawal
                var trx = account.Withdraw(request.Amount, request.Description);

                await unitOfWork.Transactions.AddAsync(trx, cancellationToken);
                await unitOfWork.Accounts.UpdateAsync(account, cancellationToken);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            var transaction = account.Transactions.OrderByDescending(t => t.CreatedAt).First();

            return new WithdrawResult(
                transaction.Id,
                account.Id,
                transaction.Amount.Amount,
                account.Balance.Amount,
                transaction.Description,
                transaction.CreatedAt,
                true,
                "Transaction completed successfully."
            );
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            return new WithdrawResult(
                Guid.Empty,
                request.AccountId,
                request.Amount,
                account.Balance.Amount,
                request.Description,
                DateTime.UtcNow,
                false,
                $"Transaction failed: {ex.Message}"
            );
        }
    }
}