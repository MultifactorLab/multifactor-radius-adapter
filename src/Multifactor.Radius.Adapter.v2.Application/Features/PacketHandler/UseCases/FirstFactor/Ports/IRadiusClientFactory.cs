using System.Net;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.FirstFactor.Ports;

public interface IRadiusClientFactory
{
    IRadiusClient CreateRadiusClient(IPEndPoint localEndpoint);
}