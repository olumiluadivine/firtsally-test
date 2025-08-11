using domain.Entities;

namespace application.Contracts.Repos;

public interface IAccountRepository
{
    Task<Account> AddAsync(Account account, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default);

    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<IEnumerable<Account>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Account?> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default);

    Task<Account?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task UpdateAsync(Account account, CancellationToken cancellationToken = default);
}