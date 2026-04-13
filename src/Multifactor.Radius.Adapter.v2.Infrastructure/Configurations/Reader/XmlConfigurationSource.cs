using Microsoft.Extensions.Configuration;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Reader;

internal sealed class XmlConfigurationSource : IConfigurationSource
{
    private readonly string _path;
    
    public XmlConfigurationSource(string path) => _path = path;
    
    public IConfigurationProvider Build(IConfigurationBuilder builder)
        => new XmlConfigurationProvider(_path);
}