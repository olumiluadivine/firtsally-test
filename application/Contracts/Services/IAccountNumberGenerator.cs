namespace application.Contracts.Services
{
    public interface IAccountNumberGenerator
    {
        Task<string> GenerateUniqueAccountNumberAsync(string prefix = "35", CancellationToken cancellationToken = default);
    }
}