namespace application.Contracts.Repos;

public interface IUnitOfWork
{
    IAccountRepository Accounts { get; }
    ITransactionRepository Transactions { get; }
    IUserRepository Users { get; }

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}