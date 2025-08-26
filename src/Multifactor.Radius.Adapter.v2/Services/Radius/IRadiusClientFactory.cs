using System.Net;

namespace Multifactor.Radius.Adapter.v2.Services.Radius;

public interface IRadiusClientFactory
{
    IRadiusClient CreateRadiusClient(IPEndPoint localEndpoint);
}