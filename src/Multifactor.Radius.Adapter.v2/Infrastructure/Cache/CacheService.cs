using Microsoft.Extensions.Caching.Memory;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Cache;

public class CacheService : ICacheService
{
    private readonly IMemoryCache _cache;

    public CacheService(IMemoryCache cache)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public void Set<T>(string key, T value, DateTimeOffset expiration)
    {
        ValidateKey(key);
        _cache.Set(key, value, expiration);
    }

    public void Set<T>(string key, T value)
    {
        ValidateKey(key);
        _cache.Set(key, value);
    }


    public bool TryGetValue<T>(string key, out T value)
    {
        ValidateKey(key);
        return _cache.TryGetValue(key, out value);
    }

    public void Remove(string key)
    {
        ValidateKey(key);
        _cache.Remove(key);
    }


    private static void ValidateKey(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
    }
}