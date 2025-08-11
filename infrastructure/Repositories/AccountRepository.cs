using application.Contracts.Repos;
using domain.Entities;
using infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace infrastructure.Repositories;

internal class AccountRepository(BankingDbContext context) : IAccountRepository
{
    public async Task<Account> AddAsync(Account account, CancellationToken cancellationToken = default)
    {
        await context.Accounts.AddAsync(account, cancellationToken);
        return account;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var account = await context.Accounts.FindAsync(new object[] { id }, cancellationToken);
        if (account != null)
        {
            context.Accounts.Remove(account);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Accounts.AnyAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default)
    {
        return await context.Accounts.AnyAsync(a => a.AccountNumber == accountNumber, cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await context.Accounts.AnyAsync(a => a.User.Email == email, cancellationToken);
    }

    public async Task<IEnumerable<Account>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await context.Accounts
            .Include(_ => _.Transactions)
            .ToListAsync(cancellationToken);
    }

    public async Task<Account?> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default)
    {
        return await context.Accounts
            .Include(_ => _.Transactions)
            .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber, cancellationToken);
    }

    public async Task<Account?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await context.Accounts
            .Include(_ => _.Transactions)
            .FirstOrDefaultAsync(a => a.User.Email == email, cancellationToken);
    }

    public async Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Accounts
            .Include(_ => _.Transactions)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public Task UpdateAsync(Account account, CancellationToken cancellationToken = default)
    {
        context.Accounts.Update(account);
        return Task.CompletedTask;
    }
}