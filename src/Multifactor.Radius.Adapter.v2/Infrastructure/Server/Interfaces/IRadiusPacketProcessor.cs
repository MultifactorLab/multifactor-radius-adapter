using Multifactor.Radius.Adapter.v2.Domain.Radius.Packet;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.Client;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Server.Interfaces;

public interface IRadiusPacketProcessor
{
    Task ProcessAsync(IRadiusPacket requestPacket, IClientConfiguration clientConfiguration);
}