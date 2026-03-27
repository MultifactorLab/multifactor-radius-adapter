using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps.FirstFactor;

public interface IFirstFactorProcessor
{
    // TODO remove 'context' from signature. Create ff request and response
    Task ProcessFirstFactor(RadiusPipelineContext context);
    AuthenticationSource AuthenticationSource { get; }
}