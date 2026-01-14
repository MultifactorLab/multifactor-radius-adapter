using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Radius.Ports;

public interface IResponseSender
{
    Task SendResponse(SendAdapterResponseRequest context);
}