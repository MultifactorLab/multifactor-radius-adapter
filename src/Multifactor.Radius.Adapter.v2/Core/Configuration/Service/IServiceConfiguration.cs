using System.Collections.ObjectModel;
using System.Net;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.RandomWaiterFeature;

namespace Multifactor.Radius.Adapter.v2.Core.Configuration.Service;

public interface IServiceConfiguration
{
    string ApiProxy { get; }
    string ApiUrl { get; }
    TimeSpan ApiTimeout { get; }
    ReadOnlyCollection<IClientConfiguration> Clients { get; }
    RandomWaiterConfig InvalidCredentialDelay { get; }
    IPEndPoint ServiceServerEndpoint { get; }
    bool SingleClientMode { get; }
    IClientConfiguration? GetClient(IPAddress ip);
    IClientConfiguration? GetClient(string nasIdentifier);
}