using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.Radius.Packet;

namespace Multifactor.Radius.Adapter.v2.Server;

public interface IRadiusPacketProcessor
{
    Task ProcessPacketAsync(IRadiusPacket requestPacket, IClientConfiguration clientConfiguration);
}