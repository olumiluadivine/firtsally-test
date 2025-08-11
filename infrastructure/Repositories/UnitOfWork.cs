using application.Contracts.Repos;
using infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace infrastructure.Repositories;

internal class UnitOfWork : IUnitOfWork, IDisposable
{
    private readonly BankingDbContext _context;
    private bool _disposed = false;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(BankingDbContext context)
    {
        _context = context;
        Users = new UserRepository(_context);
        Accounts = new AccountRepository(_context);
        Transactions = new TransactionRepository(_context);
    }

    public IAccountRepository Accounts { get; }
    public ITransactionRepository Transactions { get; }
    public IUserRepository Users { get; }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _transaction?.Dispose();
                _context.Dispose();
            }
            _disposed = true;
        }
    }
}