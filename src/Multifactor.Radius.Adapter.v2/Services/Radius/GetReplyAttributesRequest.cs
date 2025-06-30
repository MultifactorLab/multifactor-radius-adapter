using System.Globalization;
using Multifactor.Core.Ldap.Attributes;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;

namespace Multifactor.Radius.Adapter.v2.Services.Radius;

public class GetReplyAttributesRequest
{
    public IReadOnlyDictionary<string, RadiusReplyAttributeValue[]> ReplyAttributes { get; }
    public HashSet<string> UserGroups { get; }
    private IReadOnlyCollection<LdapAttribute> Attributes { get; }    
    public string UserName { get; }

    public GetReplyAttributesRequest(
        string userName,
        HashSet<string> userGroups,
        IReadOnlyDictionary<string, RadiusReplyAttributeValue[]> replyAttributes,
        IReadOnlyCollection<LdapAttribute> attributes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        ArgumentNullException.ThrowIfNull(userGroups);
        ArgumentNullException.ThrowIfNull(replyAttributes);
        ArgumentNullException.ThrowIfNull(attributes);
        
        UserName = userName;
        UserGroups = userGroups;
        ReplyAttributes = replyAttributes;
        Attributes = attributes;
    }
    
    public string? GetAttributeFirstValue(string attributeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(attributeName, nameof(attributeName));

        var name = ToLower(attributeName);
        var attribute = Attributes.FirstOrDefault(x => ToLower(x.Name) == name);
        return attribute?.GetNotEmptyValues().FirstOrDefault();
    }

    public IReadOnlyCollection<string> GetAttributeValues(string attributeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(attributeName, nameof(attributeName));
        
        var name = ToLower(attributeName);
        var attribute = Attributes.FirstOrDefault(x => ToLower(x.Name) == name);
        return attribute?.GetNotEmptyValues() ?? [];
    }
    
    public bool HasAttribute(string attributeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(attributeName, nameof(attributeName));
        var attribute = Attributes.FirstOrDefault(x => ToLower(x.Name) == ToLower(attributeName));
        return attribute is not null;
    }

    private static string ToLower(string s) => s.ToLower(CultureInfo.InvariantCulture);
}