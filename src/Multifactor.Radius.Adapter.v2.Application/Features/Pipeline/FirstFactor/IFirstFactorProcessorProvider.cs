using Multifactor.Radius.Adapter.v2.Application.Configuration.Models.Enum;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.FirstFactor;

public interface IFirstFactorProcessorProvider
{
    IFirstFactorProcessor GetProcessor(AuthenticationSource authSource);
}
