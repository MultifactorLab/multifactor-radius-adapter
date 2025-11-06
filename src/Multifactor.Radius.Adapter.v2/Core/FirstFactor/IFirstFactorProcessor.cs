using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;

namespace Multifactor.Radius.Adapter.v2.Core.FirstFactor;

public interface IFirstFactorProcessor
{
    // TODO remove 'context' from signature. Create ff request and response
    Task ProcessFirstFactor(IRadiusPipelineExecutionContext context);
    AuthenticationSource AuthenticationSource { get; }
}