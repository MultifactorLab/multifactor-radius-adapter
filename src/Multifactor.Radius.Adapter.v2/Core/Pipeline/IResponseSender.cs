using Multifactor.Radius.Adapter.v2.Services.AdapterResponseSender;

namespace Multifactor.Radius.Adapter.v2.Core.Pipeline;

public interface IResponseSender
{
    Task SendResponse(SendAdapterResponseRequest context);
}