using Multifactor.Radius.Adapter.v2.Application.Core.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.Ports;

public interface IResponseSender
{
    Task SendResponse(SendAdapterResponseRequest context);
}