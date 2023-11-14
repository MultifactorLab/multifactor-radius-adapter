//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using System.Net;
using MultiFactor.Radius.Adapter.Configuration.Features.RandomWaiterFeature;

namespace MultiFactor.Radius.Adapter.Configuration.Core
{
    public interface IServiceConfigurationBuilder
    {
        IServiceConfigurationBuilder SetApiProxy(string val);
        IServiceConfigurationBuilder SetApiUrl(string val);
        IServiceConfigurationBuilder AddClient(string nasId, IClientConfiguration client);
        IServiceConfigurationBuilder AddClient(IPAddress ip, IClientConfiguration client);
        IServiceConfigurationBuilder SetInvalidCredentialDelay(RandomWaiterConfig config);
        IServiceConfigurationBuilder SetServiceServerEndpoint(IPEndPoint endpoint);
        IServiceConfigurationBuilder IsSingleClientMode(bool single);
        IServiceConfiguration Build();
    }
}