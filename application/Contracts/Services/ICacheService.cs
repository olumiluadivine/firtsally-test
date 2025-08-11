namespace application.Contracts.Services
{
    public interface ICacheService
    {
        Task<T> GetData<T>(string key);

        Task<T> GetOrSetDataAsync<T>(string key, Func<Task<T>> fetchData, TimeSpan expirationTime);

        Task<bool> RemoveData(string key);

        Task<bool> SetDataAsync<T>(string key, T value, TimeSpan? expirationTime = null);
    }
}