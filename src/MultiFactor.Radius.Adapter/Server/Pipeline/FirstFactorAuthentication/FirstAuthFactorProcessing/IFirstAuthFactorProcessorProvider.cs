using MultiFactor.Radius.Adapter.Configuration;

namespace MultiFactor.Radius.Adapter.Server.Pipeline.FirstFactorAuthentication.FirstAuthFactorProcessing
{
    // for tests
    public interface IFirstAuthFactorProcessorProvider
    {
        IFirstAuthFactorProcessor GetProcessor(AuthenticationSource authSource);
    }
}