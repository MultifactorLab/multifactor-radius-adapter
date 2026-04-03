using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Application.SharedPorts;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.Adapters;

internal sealed class ChallengeContextCache : IChallengeContextCache
{
    private readonly IMemoryCache _cache;    
    private readonly MemoryCacheEntryOptions _cacheOptions;
    private readonly ILogger<ChallengeContextCache> _logger;
    private readonly TimeSpan _defaultCacheDuration = TimeSpan.FromMinutes(10);
    private const string CacheKeyPrefix = "Challenge";
    
    public ChallengeContextCache(IMemoryCache memoryCache, ILogger<ChallengeContextCache> logger)
    {
        _cache = memoryCache;
        _logger = logger;
        _cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _defaultCacheDuration,
            SlidingExpiration = null, //security sensitive
            Priority = CacheItemPriority.Normal,
            Size = 1,
            PostEvictionCallbacks =
            {
                new PostEvictionCallbackRegistration
                {
                    EvictionCallback = (key, value, reason, state) =>
                    {
                        if (value is RadiusPipelineContext context)
                        {
                            logger.LogDebug("Challenge context evicted: {Key}, reason: {Reason}, message id={Id}",
                                key, reason, context.RequestPacket.Identifier);
                        }
                    }
                }
            }
        };
    }

    public void Set(string key, RadiusPipelineContext value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));
        var cacheKey = $"{CacheKeyPrefix}:{key}";
        _cache.Set(cacheKey, value, _cacheOptions);
        _logger.LogInformation("Challenge {State:l} was added for message id={id} (cached until {expiration})", 
            value.ResponseInformation.State, 
            value.RequestPacket.Identifier,
            DateTime.UtcNow.Add(_cacheOptions.AbsoluteExpirationRelativeToNow!.Value));
    }

    public bool TryGetValue(string key, out RadiusPipelineContext? value)
    {        
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));

        var cacheKey = $"{CacheKeyPrefix}:{key}";
        var result = _cache.TryGetValue(cacheKey, out value);
        
        return result;
    }

    public void Remove(string key)
    {
        var cacheKey = $"{CacheKeyPrefix}:{key}";
        _cache.Remove(cacheKey);
    }
}