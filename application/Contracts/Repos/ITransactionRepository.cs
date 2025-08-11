using domain.Entities;

namespace application.Contracts.Repos;

public interface ITransactionRepository
{
    Task<Transaction> AddAsync(Transaction transaction, CancellationToken cancellationToken = default);

    Task<IEnumerable<Transaction>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IEnumerable<Transaction>> GetByAccountIdAndDateRangeAsync(
        Guid accountId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Transaction>> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default);

    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IEnumerable<Transaction>> GetMonthlyStatementAsync(
        Guid accountId,
        int year,
        int month,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default);
}