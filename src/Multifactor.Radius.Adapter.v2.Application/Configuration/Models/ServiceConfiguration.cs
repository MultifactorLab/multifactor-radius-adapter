using System.Net;

namespace Multifactor.Radius.Adapter.v2.Application.Configuration.Models;

public class ServiceConfiguration
{
    public required IRootConfiguration RootConfiguration { get; set; }
    public required IReadOnlyList<IClientConfiguration> ClientsConfigurations  { get; set; }
    public IClientConfiguration? GetClientConfiguration(string nasIdentifier) => ClientsConfigurations.FirstOrDefault(config => config.RadiusClientNasIdentifier == nasIdentifier);
    public IClientConfiguration? GetClientConfiguration(IPAddress ip)
    {
        if (SingleClientMode)
        {
            return ClientsConfigurations.FirstOrDefault();
        }
        
        return ClientsConfigurations.FirstOrDefault(config =>
            config.RadiusClientIp != null && config.RadiusClientIp.Equals(ip));
        
    }
    public bool SingleClientMode { get; set; }
}