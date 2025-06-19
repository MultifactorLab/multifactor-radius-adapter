using Multifactor.Radius.Adapter.v2.Core.Pipeline.Settings;

namespace Multifactor.Radius.Adapter.v2.Services.AuthenticatedClientCache;

public interface IAuthenticatedClientCache
{
    void SetCache(string? callingStationId, string userName, IPipelineExecutionSettings clientConfiguration);
    bool TryHitCache(string? callingStationId, string userName, IPipelineExecutionSettings clientConfiguration);
}