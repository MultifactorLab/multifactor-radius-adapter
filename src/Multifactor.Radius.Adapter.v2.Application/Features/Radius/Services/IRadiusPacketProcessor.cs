using Multifactor.Radius.Adapter.v2.Application.Configuration.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Radius.Services;

public interface IRadiusPacketProcessor
{
    Task ProcessPacketAsync(RadiusPacket requestPacket, IClientConfiguration clientConfiguration);
}