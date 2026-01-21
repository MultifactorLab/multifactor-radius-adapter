using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Cache;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Cache.AuthenticatedClientCache;

public class AuthenticatedClientCache : IAuthenticatedClientCache
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<AuthenticatedClientCache> _logger;

    public AuthenticatedClientCache(IMemoryCache memoryCache, ILogger<AuthenticatedClientCache> logger)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool TryHitCache(string? callingStationId, string userName, string clientName, TimeSpan lifetime)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientName);
        
        if (lifetime == TimeSpan.Zero)
            return false;

        if (string.IsNullOrWhiteSpace(callingStationId))
        {
            _logger.LogError("Remote host parameter miss for user {userName:l}", userName);
            return false;
        }

        var id = AuthenticatedClient.ParseId(callingStationId, clientName, userName);
        
        if (!_memoryCache.TryGetValue(id, out var cachedValue))
            return false;

        if (cachedValue is AuthenticatedClient authenticatedClient)
        {
            _logger.LogDebug($"User {userName} with calling-station-id {callingStationId} authenticated {authenticatedClient.Elapsed:hh\\:mm\\:ss} ago. Authentication session period: {lifetime}");
            return true;
        }

        return false;
    }

    public void SetCache(string? callingStationId, string? userName, string clientName, TimeSpan lifetime)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientName);
        
        if (lifetime == TimeSpan.Zero || string.IsNullOrWhiteSpace(callingStationId))
            return;

        var id = AuthenticatedClient.ParseId(callingStationId, clientName, userName);
        
        if (!_memoryCache.TryGetValue(id, out _))
        {
            var client = new AuthenticatedClient([callingStationId, clientName, userName]);
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = lifetime
            };

            _memoryCache.Set(id, client, cacheOptions);
            
            var expirationDate = DateTimeOffset.Now.Add(lifetime);
            _logger.LogDebug("Authentication for user '{userName}' is saved in cache till '{expiration}' with key '{key}'", 
                userName, expirationDate.ToString("O"), id);
        }
        else
        {
            _logger.LogDebug("Cache entry for user '{userName}' with key '{key}' already exists", userName, id);
        }
    }
}