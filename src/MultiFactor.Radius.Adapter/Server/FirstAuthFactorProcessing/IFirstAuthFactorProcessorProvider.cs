using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Core;

namespace MultiFactor.Radius.Adapter.Server.FirstAuthFactorProcessing
{
    public interface IFirstAuthFactorProcessorProvider
    {
        IFirstAuthFactorProcessor GetProcessor(AuthenticationSource authSource);
    }
}