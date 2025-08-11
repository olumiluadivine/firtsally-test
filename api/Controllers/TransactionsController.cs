using api.Controllers.Base;
using application.Commands.Transactions;
using application.Queries.Transactions;
using domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[Route("api/[controller]")]
[Authorize]
public class TransactionsController : BaseApiController
{
    private readonly ILogger<TransactionsController> _logger;
    private readonly IMediator _mediator;

    public TransactionsController(IMediator mediator, ILogger<TransactionsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Confirm a Paystack deposit after payment completion
    /// </summary>
    [HttpPost("deposit/confirm")]
    public async Task<ActionResult<ApiResponse<ConfirmDepositResult>>> ConfirmDeposit(
        [FromBody] ConfirmDepositCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Confirming deposit for payment reference: {PaymentReference}", command.PaymentReference);

            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsSuccessful)
            {
                _logger.LogInformation("Deposit confirmed successfully. Transaction ID: {TransactionId}", result.TransactionId);
                return SuccessResponse(result, "Deposit confirmed and account credited successfully");
            }
            else
            {
                _logger.LogWarning("Deposit confirmation failed: {Message}", result.Message);
                return BadRequestResponse<ConfirmDepositResult>(result.Message);
            }
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to confirm deposit for reference {PaymentReference}", command.PaymentReference);
            return BadRequestResponse<ConfirmDepositResult>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming deposit for reference {PaymentReference}", command.PaymentReference);
            return InternalServerErrorResponse<ConfirmDepositResult>("An error occurred while confirming the deposit");
        }
    }

    /// <summary>
    /// Direct deposit money to an account (admin/internal use)
    /// </summary>
    [HttpPost("deposit")]
    public async Task<ActionResult<ApiResponse<DepositResult>>> Deposit(
        [FromBody] DepositCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing direct deposit for account {AccountId}, amount: {Amount}",
                command.AccountId, command.Amount);

            var result = await _mediator.Send(command, cancellationToken);

            _logger.LogInformation("Direct deposit processed successfully. Transaction ID: {TransactionId}",
                result.TransactionId);

            return SuccessResponse(result, "Deposit processed successfully");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to process deposit for account {AccountId}", command.AccountId);
            return BadRequestResponse<DepositResult>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing deposit for account {AccountId}", command.AccountId);
            return InternalServerErrorResponse<DepositResult>("An error occurred while processing the deposit");
        }
    }

    /// <summary>
    /// Get all available banks for transfers
    /// </summary>
    [HttpGet("banks")]
    public async Task<ActionResult<ApiResponse<GetBanksResult>>> GetBanks(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving list of available banks");

            var result = await _mediator.Send(new GetBanksQuery(), cancellationToken);

            _logger.LogInformation("Successfully retrieved {BankCount} banks", result.Banks.Count());
            return SuccessResponse(result, "Banks retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving banks");
            return InternalServerErrorResponse<GetBanksResult>("An error occurred while retrieving banks");
        }
    }

    /// <summary>
    /// Get monthly statement for an account
    /// </summary>
    [HttpGet("statement/{accountId:guid}")]
    public async Task<ActionResult<ApiResponse<MonthlyStatementDto>>> GetMonthlyStatement(
        Guid accountId,
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetMonthlyStatementQuery(accountId, year, month);
            var result = await _mediator.Send(query, cancellationToken);
            return SuccessResponse(result, $"Monthly statement for {year}/{month:D2} retrieved successfully");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Account not found: {AccountId}", accountId);
            return NotFoundResponse<MonthlyStatementDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving monthly statement for account {AccountId}", accountId);
            return InternalServerErrorResponse<MonthlyStatementDto>("An error occurred while retrieving the monthly statement");
        }
    }

    /// <summary>
    /// Get transaction history for an account
    /// </summary>
    [HttpGet("history/{accountId:guid}")]
    public async Task<ActionResult<PaginatedApiResponse<TransactionDto>>> GetTransactionHistory(
        Guid accountId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetTransactionHistoryQuery(accountId, fromDate, toDate, pageNumber, pageSize);
            var result = await _mediator.Send(query, cancellationToken);

            // Assuming the result contains pagination info
            return PaginatedResponse(
                result.Transactions,
                pageNumber,
                pageSize,
                result.TotalCount,
                "Transaction history retrieved successfully");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Account not found: {AccountId}", accountId);
            // Create a proper PaginatedApiResponse for error cases
            var errorResponse = new PaginatedApiResponse<TransactionDto>
            {
                Success = false,
                Message = ex.Message,
                StatusCode = 404,
                Timestamp = DateTime.UtcNow
            };
            return NotFound(errorResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transaction history for account {AccountId}", accountId);
            // Create a proper PaginatedApiResponse for error cases
            var errorResponse = new PaginatedApiResponse<TransactionDto>
            {
                Success = false,
                Message = "An error occurred while retrieving transaction history",
                StatusCode = 500,
                Timestamp = DateTime.UtcNow
            };
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Initiate a Paystack deposit - generates payment link
    /// </summary>
    [HttpPost("deposit/initiate")]
    public async Task<ActionResult<ApiResponse<InitiateDepositResult>>> InitiateDeposit(
        [FromBody] InitiateDepositCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Initiating Paystack deposit for account {AccountId}, amount: {Amount}",
                command.AccountId, command.Amount);

            var result = await _mediator.Send(command, cancellationToken);

            _logger.LogInformation("Paystack deposit initiated successfully. Payment Reference: {PaymentReference}",
                result.PaymentReference);

            return SuccessResponse(result, "Payment link generated successfully. Use the provided URL to complete payment.");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to initiate deposit for account {AccountId}", command.AccountId);
            return BadRequestResponse<InitiateDepositResult>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating deposit for account {AccountId}", command.AccountId);
            return InternalServerErrorResponse<InitiateDepositResult>("An error occurred while initiating the deposit");
        }
    }

    /// <summary>
    /// Initiate an external withdrawal/transfer via Paystack
    /// </summary>
    [HttpPost("withdraw/external")]
    public async Task<ActionResult<ApiResponse<InitiateWithdrawalResult>>> InitiateExternalWithdrawal(
        [FromBody] InitiateWithdrawalCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Initiating external withdrawal for account {AccountId}, amount: {Amount} to {BankCode}-{AccountNumber}",
                command.AccountId, command.Amount, command.BankCode, command.AccountNumber);

            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsSuccessful)
            {
                _logger.LogInformation("External withdrawal initiated successfully. Transfer Reference: {TransferReference}", result.TransferReference);
                return SuccessResponse(result, "External transfer initiated successfully. Awaiting confirmation from Paystack.");
            }
            else
            {
                _logger.LogWarning("External withdrawal initiation failed: {Message}", result.Message);
                return BadRequestResponse<InitiateWithdrawalResult>(result.Message);
            }
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to initiate external withdrawal for account {AccountId}", command.AccountId);
            return BadRequestResponse<InitiateWithdrawalResult>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating external withdrawal for account {AccountId}", command.AccountId);
            return InternalServerErrorResponse<InitiateWithdrawalResult>("An error occurred while initiating the external withdrawal");
        }
    }

    /// <summary>
    /// Transfer money between accounts
    /// </summary>
    [HttpPost("transfer")]
    public async Task<ActionResult<ApiResponse<TransferResult>>> Transfer(
        [FromBody] TransferCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing transfer from account {FromAccountId} to {ToAccountId}, amount: {Amount}",
                command.FromAccountId, command.ToAccountId, command.Amount);

            var result = await _mediator.Send(command, cancellationToken);

            _logger.LogInformation("Transfer processed successfully. From Transaction ID: {FromTransactionId}, To Transaction ID: {ToTransactionId}",
                result.FromTransactionId, result.ToTransactionId);

            return SuccessResponse(result, "Transfer completed successfully");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to process transfer from {FromAccountId} to {ToAccountId}",
                command.FromAccountId, command.ToAccountId);
            return BadRequestResponse<TransferResult>(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized transfer attempt from account {FromAccountId}", command.FromAccountId);
            return BadRequestResponse<TransferResult>("Invalid PIN provided");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing transfer from {FromAccountId} to {ToAccountId}",
                command.FromAccountId, command.ToAccountId);
            return InternalServerErrorResponse<TransferResult>("An error occurred while processing the transfer");
        }
    }

    /// <summary>
    /// Verify account name for external transfer
    /// </summary>
    [HttpPost("verify-account")]
    public async Task<ActionResult<ApiResponse<VerifyAccountResult>>> VerifyAccount(
        [FromBody] VerifyAccountQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Verifying account {AccountNumber} for bank {BankCode}",
                query.AccountNumber, query.BankCode);

            var result = await _mediator.Send(query, cancellationToken);

            if (result.IsSuccessful)
            {
                _logger.LogInformation("Account verification successful for {AccountNumber}", query.AccountNumber);
                return SuccessResponse(result, "Account verification successful");
            }
            else
            {
                _logger.LogWarning("Account verification failed for {AccountNumber}: {Message}",
                    query.AccountNumber, result.Message);
                return BadRequestResponse<VerifyAccountResult>(result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying account {AccountNumber} for bank {BankCode}",
                query.AccountNumber, query.BankCode);
            return InternalServerErrorResponse<VerifyAccountResult>("An error occurred while verifying the account");
        }
    }

    /// <summary>
    /// Withdraw money from an account
    /// </summary>
    [HttpPost("withdraw")]
    public async Task<ActionResult<ApiResponse<WithdrawResult>>> Withdraw(
        [FromBody] WithdrawCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing withdrawal for account {AccountId}, amount: {Amount}",
                command.AccountId, command.Amount);

            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsSuccessful)
            {
                _logger.LogInformation("Withdrawal processed successfully. Transaction ID: {TransactionId}", result.TransactionId);
                return SuccessResponse(result, "Withdrawal completed successfully");
            }
            else
            {
                _logger.LogWarning("Withdrawal failed: {Message}", result.Message);
                return BadRequestResponse<WithdrawResult>(result.Message);
            }
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to process withdrawal for account {AccountId}", command.AccountId);
            return BadRequestResponse<WithdrawResult>(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized withdrawal attempt for account {AccountId}", command.AccountId);
            return BadRequestResponse<WithdrawResult>("Invalid PIN provided");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing withdrawal for account {AccountId}", command.AccountId);
            return InternalServerErrorResponse<WithdrawResult>("An error occurred while processing the withdrawal");
        }
    }
}