using application.Contracts.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace infrastructure.Services;

internal class RedisCacheService(
    IDistributedCache distributedCache,
    ILogger<RedisCacheService> logger) : ICacheService
{
    public async Task<T> GetData<T>(string key)
    {
        try
        {
            logger.LogInformation("Retrieving data from cache for key: {key}", key);
            var value = await distributedCache.GetStringAsync(key);

            if (!string.IsNullOrEmpty(value))
            {
                return JsonSerializer.Deserialize<T>(value);
            }

            return default;
        }
        catch (Exception ex)
        {
            return default;
        }
    }

    public async Task<T> GetOrSetDataAsync<T>(string key, Func<Task<T>> fetchData, TimeSpan expirationTime)
    {
        var value = await GetData<T>(key);
        if (value != null)
        {
            return value;
        }

        try
        {
            // Fetch the data
            logger.LogInformation("Cache miss for key: {key}, fetching data", key);
            value = await fetchData();

            // Check if the fetched data is not null
            if (value != null)
            {
                await SetDataAsync(key, value, expirationTime);
            }
            else
            {
                logger.LogWarning("Fetched data is null for key: {key}", key);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching data for key: {key}", key);
            throw;
        }

        return value;
    }

    public async Task<bool> RemoveData(string key)
    {
        try
        {
            logger.LogInformation("Checking for data existence in cache for key: {key}", key);
            var exist = await distributedCache.GetStringAsync(key);

            if (!string.IsNullOrEmpty(exist))
            {
                logger.LogInformation("Removing data from cache for key: {key}", key);
                await distributedCache.RemoveAsync(key);
                return true;
            }

            logger.LogInformation("Data not found in cache for key: {key}", key);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing data from cache for key: {key}", key);
            throw;
        }
    }

    public async Task<bool> SetDataAsync<T>(string key, T value, TimeSpan? expirationTime = null)
    {
        try
        {
            logger.LogInformation("Setting data in cache for key: {key} with expiration: {expirationTime}", key, expirationTime);

            var options = new DistributedCacheEntryOptions();

            if (expirationTime.HasValue)
            {
                options.SetAbsoluteExpiration(DateTimeOffset.UtcNow.Add(expirationTime.Value));
            }

            await distributedCache.SetStringAsync(key, JsonSerializer.Serialize(value), options);
            logger.LogInformation("Data set successfully in cache for key: {key}", key);
            return true; // Indicate successful setting
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error setting data in cache for key: {key}", key);
            throw;
        }
    }
}