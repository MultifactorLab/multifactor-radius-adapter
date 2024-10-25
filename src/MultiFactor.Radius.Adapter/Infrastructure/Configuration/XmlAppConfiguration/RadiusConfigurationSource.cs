using System.Text.RegularExpressions;

namespace MultiFactor.Radius.Adapter.Infrastructure.Configuration.XmlAppConfiguration;

public abstract class RadiusConfigurationSource
{
    /// <summary>
    /// Source name.
    /// </summary>
    public abstract string Name { get; }

    public override bool Equals(object obj)
    {
        if (obj is not RadiusConfigurationSource rad)
        {
            return false;
        }

        if (ReferenceEquals(obj, this))
        {
            return true;
        }
        
        return Name == rad.Name;
    }

    public override int GetHashCode() => Name.GetHashCode();
    
    public static string TransformName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }
        
        name = Regex.Replace(name, @"\s+", string.Empty);
        return name;
    }
}