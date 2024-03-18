using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Server.Pipeline.FirstFactorAuthentication;

namespace MultiFactor.Radius.Adapter.Server.Pipeline.FirstFactorAuthentication.FirstAuthFactorProcessing
{
    public interface IFirstAuthFactorProcessorProvider
    {
        IFirstAuthFactorProcessor GetProcessor(AuthenticationSource authSource);
    }
}