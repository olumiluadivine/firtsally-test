using application.Contracts.Repos;
using domain.Entities;
using infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace infrastructure.Repositories;

internal class TransactionRepository(BankingDbContext context) : ITransactionRepository
{
    public async Task<Transaction> AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        await context.Transactions.AddAsync(transaction, cancellationToken);
        return transaction;
    }

    public async Task<IEnumerable<Transaction>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await context.Transactions
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Transaction>> GetByAccountIdAndDateRangeAsync(
        Guid accountId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        return await context.Transactions
            .Where(t => t.AccountId == accountId && t.CreatedAt >= from && t.CreatedAt <= to)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Transaction>> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await context.Transactions
            .Where(t => t.AccountId == accountId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Transactions
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Transaction>> GetMonthlyStatementAsync(
        Guid accountId,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var from = new DateTime(year, month, 1);
        var to = from.AddMonths(1).AddDays(-1);

        return await GetByAccountIdAndDateRangeAsync(accountId, from, to, cancellationToken);
    }

    public Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        context.Transactions.Update(transaction);
        return Task.CompletedTask;
    }
}