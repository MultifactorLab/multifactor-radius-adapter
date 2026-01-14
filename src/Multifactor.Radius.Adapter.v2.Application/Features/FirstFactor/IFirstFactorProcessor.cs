using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;
using Multifactor.Radius.Adapter.v2.Application.Models.Enum;

namespace Multifactor.Radius.Adapter.v2.Application.Features.FirstFactor;

public interface IFirstFactorProcessor
{
    // TODO remove 'context' from signature. Create ff request and response
    Task ProcessFirstFactor(RadiusPipelineContext context);
    AuthenticationSource AuthenticationSource { get; }
}