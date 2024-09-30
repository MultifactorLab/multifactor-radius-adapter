using System;

namespace MultiFactor.Radius.Adapter.Infrastructure.Configuration.XmlAppConfiguration;

public sealed class RadiusConfigurationEnvironmentVariable : RadiusConfigurationSource
{
    public override string Name { get; }

    public RadiusConfigurationEnvironmentVariable(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
        }

        Name = name;
    }
    
    public override string ToString() => Name;
}