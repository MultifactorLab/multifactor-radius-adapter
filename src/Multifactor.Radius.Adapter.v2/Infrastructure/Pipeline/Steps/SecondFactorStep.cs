using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Steps;

public class SecondFactorStep : IRadiusPipelineStep
{
    public Task ExecuteAsync(IRadiusPipelineExecutionContext context)
    {
        context.AuthenticationState.SecondFactorStatus = AuthenticationStatus.Accept;
        return Task.CompletedTask;
    }
}