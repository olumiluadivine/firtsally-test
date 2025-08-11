using application.Commands.Accounts;
using application.Contracts.Managers;
using application.Contracts.Repos;
using application.Contracts.Services;
using application.Queries.Accounts;
using domain.Entities;

namespace infrastructure.Managers;

internal class AccountManager
    (IUnitOfWork unitOfWork,
    IEncryptionService encryptionService) : IAccountManager
{
    public async Task<ChangePINResult> ChangePin(ChangePINCommand request, CancellationToken cancellationToken)
    {
        var account = await unitOfWork.Accounts.GetByIdAsync(request.AccountId, cancellationToken);
        if (account == null)
        {
            throw new InvalidOperationException($"Account with ID '{request.AccountId}' not found.");
        }

        // Verify current PIN
        if (!encryptionService.VerifyPIN(request.CurrentPIN, account.PINHash))
        {
            throw new UnauthorizedAccessException("Current PIN is incorrect.");
        }

        // Hash new PIN
        var newPINHash = encryptionService.HashPIN(request.NewPIN);

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            account.UpdatePIN(newPINHash);

            await unitOfWork.Accounts.UpdateAsync(account, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            return new ChangePINResult(
                true,
                "PIN updated successfully",
                account.UpdatedAt);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<CreateAccountResult> CreateAccount(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        // Check if account with email already exists
        var existingAccount = await unitOfWork.Accounts.GetByEmailAsync(request.Email, cancellationToken);
        if (existingAccount != null)
        {
            throw new InvalidOperationException($"Account with email '{request.Email}' already exists.");
        }

        var newPINHash = encryptionService.HashPIN(request.PIN);

        // Create new account
        var account = new Account(
            request.UserId,
            request.Email,
            request.AccountType,
            newPINHash);

        await unitOfWork.Accounts.AddAsync(account, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateAccountResult(
            account.Id,
            account.AccountNumber,
            account.AccountType,
            account.CreatedAt);
    }

    public async Task<AccountDetailsDto> GetAccount(GetAccountDetailsQuery request, CancellationToken cancellationToken)
    {
        var account = await unitOfWork.Accounts.GetByIdAsync(request.AccountId, cancellationToken);
        if (account == null)
        {
            throw new InvalidOperationException($"Account with ID '{request.AccountId}' not found.");
        }

        return new AccountDetailsDto(
            account.Id,
            account.AccountNumber,
            account.Balance.Amount,
            account.Balance.Currency,
            account.AccountType,
            account.CreatedAt,
            account.IsActive);
    }
}