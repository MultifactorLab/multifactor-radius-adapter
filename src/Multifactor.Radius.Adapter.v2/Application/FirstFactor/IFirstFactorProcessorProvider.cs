using Multifactor.Radius.Adapter.v2.Domain.Auth;

namespace Multifactor.Radius.Adapter.v2.Application.FirstFactor;

public interface IFirstFactorProcessorProvider
{
    IFirstFactorProcessor GetProcessor(AuthenticationSource authSource);
}
