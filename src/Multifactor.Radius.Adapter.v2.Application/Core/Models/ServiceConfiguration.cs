using System.Net;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Abstractions;

namespace Multifactor.Radius.Adapter.v2.Application.Core.Models;

public sealed class ServiceConfiguration
{
    public required IRootConfiguration RootConfiguration { get; init; }
    public required IReadOnlyList<IClientConfiguration> ClientsConfigurations  { get; init; }
    public bool IsRootClientMode { get; init; }
    public IClientConfiguration? GetClientConfiguration(string nasIdentifier) 
    {
        if (IsRootClientMode)
        {
            return ClientsConfigurations[0];
        }
        return ClientsConfigurations.FirstOrDefault(config => config.RadiusClientNasIdentifier == nasIdentifier);
    } 
    
    public IClientConfiguration? GetClientConfiguration(IPAddress ip)
    {
        if (IsRootClientMode)
        {
            return ClientsConfigurations[0];
        }
        
        return ClientsConfigurations.FirstOrDefault(config =>
            config.RadiusClientIps.Any() && config.RadiusClientIps.Contains(ip));
    }
}