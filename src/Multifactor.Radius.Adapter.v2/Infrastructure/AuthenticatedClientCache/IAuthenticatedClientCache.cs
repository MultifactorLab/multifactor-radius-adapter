using Multifactor.Radius.Adapter.v2.Domain.Auth;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.AuthenticatedClientCache;

public interface IAuthenticatedClientCache
{
    void SetCache(string? callingStationId, string userName, string clientName, AuthenticatedClientCacheConfig clientConfiguration);
    bool TryHitCache(string? callingStationId, string userName, string clientName, AuthenticatedClientCacheConfig cacheConfig);
}