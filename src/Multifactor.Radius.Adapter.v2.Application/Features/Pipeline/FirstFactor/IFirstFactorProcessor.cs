using Multifactor.Radius.Adapter.v2.Application.Core.Models.Enum;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.FirstFactor;

public interface IFirstFactorProcessor
{
    // TODO remove 'context' from signature. Create ff request and response
    Task ProcessFirstFactor(RadiusPipelineContext context);
    AuthenticationSource AuthenticationSource { get; }
}