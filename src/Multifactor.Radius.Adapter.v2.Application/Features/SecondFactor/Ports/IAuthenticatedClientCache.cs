namespace Multifactor.Radius.Adapter.v2.Application.Features.SecondFactor.Ports;

public interface IAuthenticatedClientCache
{
    void SetCache(string? callingStationId, string userName, string clientName, TimeSpan lifetime);
    bool TryHitCache(string? callingStationId, string userName, string clientName, TimeSpan lifetime);
}