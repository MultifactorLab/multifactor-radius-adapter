//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Configuration.Features.RandomWaiterFeature;
using MultiFactor.Radius.Adapter.Server;
using System.Collections.Generic;
using System.Net;

namespace MultiFactor.Radius.Adapter.Configuration.Core
{
    public interface IServiceConfiguration
    {
        string ApiProxy { get; }
        string ApiUrl { get; }
        IReadOnlyList<IClientConfiguration> Clients { get; }
        RandomWaiterConfig InvalidCredentialDelay { get; }
        IPEndPoint ServiceServerEndpoint { get; }
        bool SingleClientMode { get; }

        IClientConfiguration GetClient(IPAddress ip);
        IClientConfiguration GetClient(PendingRequest request);
        IClientConfiguration GetClient(string nasIdentifier);
    }
}