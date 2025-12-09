using Multifactor.Radius.Adapter.v2.Infrastructure.Radius.Dto;

namespace Multifactor.Radius.Adapter.v2.Domain.Pipeline;

public interface IResponseSender
{
    Task SendResponse(SendAdapterResponseRequest context);
}