using Multifactor.Radius.Adapter.v2.Application.Configuration.Models;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Parser;

public interface IConfigurationParser
{
    Task<RootConfiguration> ParseRootConfigAsync(string filePath, CancellationToken ct);
    Task<ClientConfiguration> ParseClientConfigAsync(string filePath, CancellationToken ct);
}