using Multifactor.Radius.Adapter.v2.Core.Auth;

namespace Multifactor.Radius.Adapter.v2.Core.FirstFactor;

public interface IFirstFactorProcessorProvider
{
    IFirstFactorProcessor GetProcessor(AuthenticationSource authSource);
}
