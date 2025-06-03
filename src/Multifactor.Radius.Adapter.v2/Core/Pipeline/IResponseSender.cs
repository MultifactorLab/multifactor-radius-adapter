using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;

namespace Multifactor.Radius.Adapter.v2.Core.Pipeline;

public interface IResponseSender
{
    Task SendResponse(IRadiusPipelineExecutionContext context);
}