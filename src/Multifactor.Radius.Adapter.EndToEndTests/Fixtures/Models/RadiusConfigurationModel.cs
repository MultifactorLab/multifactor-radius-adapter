using MultiFactor.Radius.Adapter.Infrastructure.Configuration.XmlAppConfiguration;

namespace Multifactor.Radius.Adapter.EndToEndTests.Fixtures.Models;

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