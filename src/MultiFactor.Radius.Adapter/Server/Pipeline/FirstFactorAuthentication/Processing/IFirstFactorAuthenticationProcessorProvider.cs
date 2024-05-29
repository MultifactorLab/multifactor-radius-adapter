using MultiFactor.Radius.Adapter.Infrastructure.Configuration;

namespace MultiFactor.Radius.Adapter.Server.Pipeline.FirstFactorAuthentication.Processing
{
    // for tests
    public interface IFirstFactorAuthenticationProcessorProvider
    {
        IFirstFactorAuthenticationProcessor GetProcessor(AuthenticationSource authSource);
    }
}