using System.Net;

namespace Multifactor.Radius.Adapter.v2.Application.Configuration.Models;

public class ServiceConfiguration
{
    public required IRootConfiguration RootConfiguration { get; init; }
    public required IReadOnlyList<IClientConfiguration> ClientsConfigurations  { get; init; }
    public bool isRootClientMode { get; init; }
    public IClientConfiguration? GetClientConfiguration(string nasIdentifier) 
    {
        if (isRootClientMode)
        {
            return ClientsConfigurations.FirstOrDefault();
        }

        return ClientsConfigurations.FirstOrDefault(config => config.RadiusClientNasIdentifier == nasIdentifier);
    } 
    public IClientConfiguration? GetClientConfiguration(IPAddress ip)
    {
        if (isRootClientMode)
        {
            return ClientsConfigurations.FirstOrDefault();
        }
        
        return ClientsConfigurations.FirstOrDefault(config =>
            config.RadiusClientIps != null && config.RadiusClientIps.Any() && config.RadiusClientIps.Contains(ip));
        
    }
}