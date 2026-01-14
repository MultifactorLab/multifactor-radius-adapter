using Multifactor.Radius.Adapter.v2.Application.Models.Enum;

namespace Multifactor.Radius.Adapter.v2.Application.Features.FirstFactor;

public interface IFirstFactorProcessorProvider
{
    IFirstFactorProcessor GetProcessor(AuthenticationSource authSource);
}
