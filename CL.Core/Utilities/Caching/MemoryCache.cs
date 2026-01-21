using System.Collections.Concurrent;

namespace CL.Core.Utilities.Caching;

/// <summary>
/// In-memory cache implementation with expiration support.
/// Thread-safe and suitable for single-instance applications.
/// </summary>
public class MemoryCache : ICache
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly CacheOptions _options;
    private long _hits;
    private long _misses;

    /// <summary>
    /// Creates an in-memory cache with optional configuration.
    /// </summary>
    /// <param name="options">Cache options or null to use defaults.</param>
    public MemoryCache(CacheOptions? options = null)
    {
        _options = options ?? new CacheOptions();

        // Start cleanup task if auto-cleanup is enabled
        if (_options.AutoCleanupInterval.HasValue)
        {
            _ = Task.Run(AutoCleanupLoop);
        }
    }

    /// <summary>
    /// Stores a value in the cache with optional expiration.
    /// </summary>
    /// <param name="key">Cache key.</param>
    /// <param name="value">Value to store.</param>
    /// <param name="expiration">Optional expiration interval.</param>
    /// <returns>True when the value is stored successfully.</returns>
    public Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        try
        {
            var expiresAt = expiration.HasValue
                ? DateTime.UtcNow.Add(expiration.Value)
                : _options.DefaultExpiration.HasValue
                    ? DateTime.UtcNow.Add(_options.DefaultExpiration.Value)
                    : (DateTime?)null;

            var entry = new CacheEntry
            {
                Value = value,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow
            };

            _cache[key] = entry;

            // Check size limit
            if (_options.MaxItems > 0 && _cache.Count > _options.MaxItems)
            {
                RemoveOldestEntry();
            }

            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Gets a cached value by key.
    /// </summary>
    /// <param name="key">Cache key.</param>
    /// <returns>Cached value or default when missing or expired.</returns>
    public Task<T?> GetAsync<T>(string key)
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            // Check if expired
            if (entry.ExpiresAt.HasValue && DateTime.UtcNow > entry.ExpiresAt.Value)
            {
                _cache.TryRemove(key, out _);
                Interlocked.Increment(ref _misses);
                return Task.FromResult<T?>(default);
            }

            Interlocked.Increment(ref _hits);

            if (entry.Value is T typedValue)
            {
                return Task.FromResult<T?>(typedValue);
            }
        }

        Interlocked.Increment(ref _misses);
        return Task.FromResult<T?>(default);
    }

    /// <summary>
    /// Checks whether a cache entry exists and is not expired.
    /// </summary>
    /// <param name="key">Cache key.</param>
    /// <returns>True if the entry exists and is valid.</returns>
    public Task<bool> ExistsAsync(string key)
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            // Check if expired
            if (entry.ExpiresAt.HasValue && DateTime.UtcNow > entry.ExpiresAt.Value)
            {
                _cache.TryRemove(key, out _);
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    /// <summary>
    /// Removes a cache entry by key.
    /// </summary>
    /// <param name="key">Cache key.</param>
    /// <returns>True if the entry was removed.</returns>
    public Task<bool> RemoveAsync(string key)
    {
        var removed = _cache.TryRemove(key, out _);
        return Task.FromResult(removed);
    }

    /// <summary>
    /// Clears all cache entries and resets statistics.
    /// </summary>
    public Task ClearAsync()
    {
        _cache.Clear();
        Interlocked.Exchange(ref _hits, 0);
        Interlocked.Exchange(ref _misses, 0);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets a cached value or creates and stores one when missing.
    /// </summary>
    /// <param name="key">Cache key.</param>
    /// <param name="factory">Factory used to create the value when missing.</param>
    /// <param name="expiration">Optional expiration interval.</param>
    /// <returns>Cached or newly created value.</returns>
    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
    {
        var existing = await GetAsync<T>(key);

        if (existing != null)
        {
            return existing;
        }

        var value = await factory();
        await SetAsync(key, value, expiration);

        return value;
    }

    /// <summary>
    /// Gets cache statistics including counts and hit/miss totals.
    /// </summary>
    /// <returns>Snapshot of cache statistics.</returns>
    public CacheStatistics GetStatistics()
    {
        // Clean up expired entries before getting count
        CleanupExpiredEntries();

        return new CacheStatistics
        {
            ItemCount = _cache.Count,
            Hits = Interlocked.Read(ref _hits),
            Misses = Interlocked.Read(ref _misses)
        };
    }

    private void RemoveOldestEntry()
    {
        var oldest = _cache
            .OrderBy(x => x.Value.CreatedAt)
            .FirstOrDefault();

        if (!oldest.Equals(default(KeyValuePair<string, CacheEntry>)))
        {
            _cache.TryRemove(oldest.Key, out _);
        }
    }

    private void CleanupExpiredEntries()
    {
        var now = DateTime.UtcNow;
        var expiredKeys = _cache
            .Where(x => x.Value.ExpiresAt.HasValue && now > x.Value.ExpiresAt.Value)
            .Select(x => x.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _cache.TryRemove(key, out _);
        }
    }

    private async Task AutoCleanupLoop()
    {
        while (true)
        {
            try
            {
                await Task.Delay(_options.AutoCleanupInterval!.Value);
                CleanupExpiredEntries();
            }
            catch
            {
                // Continue cleanup loop even if one iteration fails
            }
        }
    }

    private class CacheEntry
    {
        /// <summary>
        /// Cached value.
        /// </summary>
        public required object? Value { get; init; }

        /// <summary>
        /// Optional expiration timestamp.
        /// </summary>
        public DateTime? ExpiresAt { get; init; }

        /// <summary>
        /// Timestamp when the entry was created.
        /// </summary>
        public DateTime CreatedAt { get; init; }
    }
}

/// <summary>
/// Configuration options for the cache.
/// </summary>
public class CacheOptions
{
    /// <summary>
    /// Maximum number of items to store in cache.
    /// When exceeded, oldest items are removed.
    /// 0 = unlimited (default: 10000)
    /// </summary>
    public int MaxItems { get; set; } = 10000;

    /// <summary>
    /// Default expiration time for cached items.
    /// null = no expiration (default: 1 hour)
    /// </summary>
    public TimeSpan? DefaultExpiration { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Interval for automatic cleanup of expired items.
    /// null = no automatic cleanup (default: 5 minutes)
    /// </summary>
    public TimeSpan? AutoCleanupInterval { get; set; } = TimeSpan.FromMinutes(5);
}
