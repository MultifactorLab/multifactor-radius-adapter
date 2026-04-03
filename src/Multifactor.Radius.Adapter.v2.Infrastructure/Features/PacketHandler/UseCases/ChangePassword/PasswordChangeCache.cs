using Microsoft.Extensions.Caching.Memory;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SharedServices.ChallengeProcessor.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SharedServices.ChallengeProcessor.Ports;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.UseCases.ChangePassword;

internal sealed class PasswordChangeCache : IPasswordChangeCache
{
    private readonly IMemoryCache _cache;
    private const string CacheKeyPrefix = "Password";

    public PasswordChangeCache(IMemoryCache memoryCache)
    {
        _cache = memoryCache;
    }
    public void Set(string key, PasswordChangeValue value, DateTimeOffset expirationDate)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));
        var cacheKey = $"{CacheKeyPrefix}:{key}";
        _cache.Set(cacheKey, value, expirationDate);
    }

    public bool TryGetValue(string key, out PasswordChangeValue? value)
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