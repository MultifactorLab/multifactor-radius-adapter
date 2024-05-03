//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Configuration.Features.RandomWaiterFeature;
using MultiFactor.Radius.Adapter.Framework.Context;
using System;
using System.Collections.ObjectModel; 
using System.Net;

namespace MultiFactor.Radius.Adapter.Configuration.Core
{
    public interface IServiceConfiguration
    {
        string ApiProxy { get; }
        string ApiUrl { get; }
        TimeSpan ApiTimeout{ get; }
        ReadOnlyCollection<IClientConfiguration> Clients { get; }
        RandomWaiterConfig InvalidCredentialDelay { get; }
        IPEndPoint ServiceServerEndpoint { get; }
        bool SingleClientMode { get; }

        IClientConfiguration GetClient(IPAddress ip);
        IClientConfiguration GetClient(RadiusContext request);
        IClientConfiguration GetClient(string nasIdentifier);
    }
}