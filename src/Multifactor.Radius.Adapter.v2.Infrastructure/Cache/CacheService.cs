using Microsoft.Extensions.Caching.Memory;
using Multifactor.Radius.Adapter.v2.Application.Cache;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Cache;

public class CacheService : ICacheService
{
    private readonly IMemoryCache _cache;

    public CacheService(IMemoryCache memoryCache)
    {
        _cache = memoryCache;
    }

    public void Set<T>(string key, T value, DateTimeOffset expirationDate)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));
        
        _cache.Set(key, value, expirationDate);
    }

    public bool TryGetValue<T>(string key, out T? value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));

        var result = _cache.TryGetValue(key, out value);
        
        return result;
    }

    public void Remove(string key)
    {
        _cache.Remove(key);
    }
}