using System.Net;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Radius.Interfaces;

public interface IRadiusClientFactory
{
    IRadiusClient Create(IPEndPoint localEndpoint);
}