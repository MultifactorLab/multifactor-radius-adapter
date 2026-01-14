using System.Xml.Linq;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Reader;

public interface IXmlReader
{
    Task<XDocument> ReadAsync(string filePath, CancellationToken cancellationToken);
    IReadOnlyDictionary<string, string> ExtractAppSettings(XDocument xml);
    IReadOnlyList<XElement> GetLdapServerElements(XDocument xml);
    IReadOnlyList<XElement> GetRadiusReplyElements(XDocument xml);
}