namespace CL.Core.Utilities.Caching;

/// <summary>
/// Abstraction for caching operations with memory-based storage.
/// </summary>
public interface ICache
{
    /// <summary>
    /// Stores a value in the cache.
    /// </summary>
    /// <typeparam name="T">Type of value to cache</typeparam>
    /// <param name="key">Unique cache key</param>
    /// <param name="value">Value to store</param>
    /// <param name="expiration">Optional expiration time</param>
    /// <returns>True if stored successfully</returns>
    Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiration = null);

    /// <summary>
    /// Retrieves a value from the cache.
    /// </summary>
    /// <typeparam name="T">Type of value to retrieve</typeparam>
    /// <param name="key">Cache key</param>
    /// <returns>Cached value or default if not found/expired</returns>
    Task<T?> GetAsync<T>(string key);

    /// <summary>
    /// Checks if a key exists in the cache and is not expired.
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <returns>True if key exists and is valid</returns>
    Task<bool> ExistsAsync(string key);

    /// <summary>
    /// Removes a value from the cache.
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <returns>True if removed successfully</returns>
    Task<bool> RemoveAsync(string key);

    /// <summary>
    /// Clears all values from the cache.
    /// </summary>
    Task ClearAsync();

    /// <summary>
    /// Gets or creates a cached value using a factory function.
    /// </summary>
    /// <typeparam name="T">Type of value</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="factory">Factory function to create value if not cached</param>
    /// <param name="expiration">Optional expiration time</param>
    /// <returns>Cached or newly created value</returns>
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);

    /// <summary>
    /// Gets statistics about the cache.
    /// </summary>
    CacheStatistics GetStatistics();
}

/// <summary>
/// Cache statistics for monitoring.
/// </summary>
public class CacheStatistics
{
    /// <summary>
    /// Total number of items in cache
    /// </summary>
    public int ItemCount { get; set; }

    /// <summary>
    /// Number of cache hits
    /// </summary>
    public long Hits { get; set; }

    /// <summary>
    /// Number of cache misses
    /// </summary>
    public long Misses { get; set; }

    /// <summary>
    /// Cache hit ratio (0-100)
    /// </summary>
    public double HitRatio => (Hits + Misses) > 0 ? (double)Hits / (Hits + Misses) * 100 : 0;
}
