using System.Collections.ObjectModel;
using System.Net;
using Multifactor.Radius.Adapter.v2.Domain;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.Client;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.Service;

public interface IServiceConfiguration
{
    string ApiProxy { get; }
    IReadOnlyList<string> ApiUrls { get; }
    TimeSpan ApiTimeout { get; }
    ReadOnlyCollection<IClientConfiguration> Clients { get; }
    RandomWaiterConfig InvalidCredentialDelay { get; }
    IPEndPoint ServiceServerEndpoint { get; }
    bool SingleClientMode { get; }
    IClientConfiguration? GetClient(IPAddress ip);
    IClientConfiguration? GetClient(string nasIdentifier);
}