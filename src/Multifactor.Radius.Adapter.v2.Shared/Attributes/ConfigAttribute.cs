namespace Multifactor.Radius.Adapter.v2.Shared.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ConfigParameterAttribute : Attribute
{
    public string XmlName { get; }
    public string EnvName { get; }
    public object? DefaultValue { get; }
    public bool Required { get; }
    
    public ConfigParameterAttribute(string xmlName, object? defaultValue = null)
    {
        XmlName = xmlName;
        EnvName = ConvertToEnvName(xmlName);
        DefaultValue = defaultValue;
    }
    
    private static string ConvertToEnvName(string xmlName)
    {
        return xmlName.Replace('-', '_').ToUpperInvariant();
    }
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ComplexConfigParameterAttribute : Attribute
{
    public string XmlElementName { get; }
    
    public ComplexConfigParameterAttribute(string xmlElementName)
    {
        XmlElementName = xmlElementName;
    }
}