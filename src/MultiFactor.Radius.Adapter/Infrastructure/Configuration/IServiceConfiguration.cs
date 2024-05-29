//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Core.Framework.Context;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Features.RandomWaiterFeature;
using System;
using System.Collections.ObjectModel;
using System.Net;

namespace MultiFactor.Radius.Adapter.Infrastructure.Configuration
{
    public interface IServiceConfiguration
    {
        string ApiProxy { get; }
        string ApiUrl { get; }
        TimeSpan ApiTimeout { get; }
        ReadOnlyCollection<IClientConfiguration> Clients { get; }
        RandomWaiterConfig InvalidCredentialDelay { get; }
        IPEndPoint ServiceServerEndpoint { get; }
        bool SingleClientMode { get; }

        IClientConfiguration GetClient(IPAddress ip);
        IClientConfiguration GetClient(RadiusContext request);
        IClientConfiguration GetClient(string nasIdentifier);
    }
}