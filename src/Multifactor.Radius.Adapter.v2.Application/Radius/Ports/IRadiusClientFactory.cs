using System.Net;

namespace Multifactor.Radius.Adapter.v2.Application.Radius.Ports;

public interface IRadiusClientFactory
{
    IRadiusClient CreateRadiusClient(IPEndPoint localEndpoint);
}