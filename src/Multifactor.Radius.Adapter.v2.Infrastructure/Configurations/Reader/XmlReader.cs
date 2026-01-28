using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Exceptions;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Reader;

public static class XmlReader
{
    public static async Task<XDocument> ReadAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = File.OpenRead(filePath);
            var settings = new XmlReaderSettings
            {
                Async = true,
                IgnoreComments = true,
                IgnoreWhitespace = true
            };
            
            using var xmlReader = System.Xml.XmlReader.Create(stream, settings);
            var document = await XDocument.LoadAsync(xmlReader, LoadOptions.None, cancellationToken);
            
            return document;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidConfigurationException($"Failed to read configuration file: {filePath}", ex);
        }
    }
    
    public static IReadOnlyDictionary<string, string> ExtractAppSettings(XDocument xml)
    {
        var appSettings = xml.Root?.Element("appSettings");
        if (appSettings == null)
            return new Dictionary<string, string>();
        
        return appSettings.Elements("add")
            .Where(e => e.Attribute("key") != null && e.Attribute("value") != null)
            .ToDictionary(
                e => e.Attribute("key")!.Value,
                e => e.Attribute("value")!.Value,
                StringComparer.OrdinalIgnoreCase);
    }
    
    public static IReadOnlyList<XElement> GetLdapServerElements(XDocument xml)
    {
        return xml.Root?.Element("ldapServers")?.Elements("ldapServer").ToList() 
            ?? [];
    }
    
    public static IReadOnlyList<XElement> GetRadiusReplyElements(XDocument xml)
    {
        var attributes = xml.Root?.Element("RadiusReply")?.Element("Attributes");
        return attributes?.Elements("add").ToList() ?? [];
    }
}