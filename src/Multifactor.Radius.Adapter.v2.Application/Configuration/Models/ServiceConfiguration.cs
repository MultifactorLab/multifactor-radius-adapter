using System.Net;

namespace Multifactor.Radius.Adapter.v2.Application.Configuration.Models;

public class ServiceConfiguration
{
    public required RootConfiguration RootConfiguration { get; set; }
    public required IReadOnlyList<ClientConfiguration> ClientsConfigurations  { get; set; }
    public ClientConfiguration GetClientConfiguration(string nasIdentifier) => ClientsConfigurations.FirstOrDefault(config => config.RadiusClientNasIdentifier == nasIdentifier);
    public ClientConfiguration GetClientConfiguration(IPAddress ip) => ClientsConfigurations.FirstOrDefault(config => config.RadiusClientIp != null && config.RadiusClientIp.Equals(ip));
}