using Multifactor.Radius.Adapter.v2.Core.Auth;

namespace Multifactor.Radius.Adapter.v2.Services.AuthenticatedClientCache;

public interface IAuthenticatedClientCache
{
    void SetCache(string? callingStationId, string userName, string clientName, AuthenticatedClientCacheConfig clientConfiguration);
    bool TryHitCache(string? callingStationId, string userName, string clientName, AuthenticatedClientCacheConfig cacheConfig);
}