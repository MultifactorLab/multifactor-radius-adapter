using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.XmlAppConfiguration;

namespace Multifactor.Radius.Adapter.v2.EndToEndTests.Fixtures.Models;

public class RadiusConfigurationModel : RadiusConfigurationSource
{
    public override string Name { get; }

    public RadiusConfigurationModel(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(nameof(name));
        
        Name = name;
    }
}