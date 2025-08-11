using application.Contracts.Repos;
using application.Contracts.Services;

namespace infrastructure.Services;

internal class AccountNumberGenerator : IAccountNumberGenerator
{
    private readonly IAccountRepository _accountRepository;

    public AccountNumberGenerator(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<string> GenerateUniqueAccountNumberAsync(string prefix = "35", CancellationToken cancellationToken = default)
    {
        const int maxAttempts = 10;
        var attempts = 0;

        while (attempts < maxAttempts)
        {
            var accountNumber = GenerateAccountNumber(prefix);

            var exists = await _accountRepository.ExistsByAccountNumberAsync(accountNumber, cancellationToken);

            if (!exists)
                return accountNumber;

            attempts++;
        }

        // If we can't generate a unique number after max attempts, use timestamp-based approach
        return GenerateTimestampBasedAccountNumber(prefix);
    }

    private static string GenerateAccountNumber(string prefix)
    {
        var number = Random.Shared.Next(10000000, 99999999);
        return $"{prefix}{number:D8}";
    }

    private static string GenerateTimestampBasedAccountNumber(string prefix)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = Random.Shared.Next(1000, 9999);
        return $"{prefix}{timestamp}{random}";
    }
}