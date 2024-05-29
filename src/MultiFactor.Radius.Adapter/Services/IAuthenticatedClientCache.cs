using MultiFactor.Radius.Adapter.Infrastructure.Configuration;

namespace MultiFactor.Radius.Adapter.Services
{
    public interface IAuthenticatedClientCache
    {
        void SetCache(string callingStationId, string userName, IClientConfiguration clientConfiguration);
        bool TryHitCache(string callingStationId, string userName, IClientConfiguration clientConfiguration);
    }
}