using Microsoft.Extensions.Caching.Memory;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.Ports;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.Adapters;

internal sealed class PacketKeyCache : IPacketKeyCache
{
    private readonly IMemoryCache _cache;
    private const string CacheKeyPrefix = "PacketKey";

    public PacketKeyCache(IMemoryCache memoryCache)
    {
        _cache = memoryCache;
    }

    public void Set(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));
        var cacheKey = $"{CacheKeyPrefix}:{key}";
        _cache.Set(cacheKey, 0, new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(1),
            Size = 1
        });
    }

    public bool HasValue(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));       
        var cacheKey = $"{CacheKeyPrefix}:{key}";

        return _cache.TryGetValue(cacheKey, out _);
    }
}