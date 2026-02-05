using System.Globalization;
using Multifactor.Core.Ldap.Attributes;
using Multifactor.Radius.Adapter.v2.Application.Configuration.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;

public class GetReplyAttributesRequest
{
    public string? UserName { get; }
    public HashSet<string> UserGroups { get; }
    public IReadOnlyDictionary<string, IRadiusReplyAttribute[]> ReplyAttributes { get; }
    private IReadOnlyCollection<LdapAttribute> Attributes { get; }  
    
    public GetReplyAttributesRequest(
        string? userName,
        HashSet<string> userGroups,
        IReadOnlyDictionary<string, IRadiusReplyAttribute[]> replyAttributes,
        IReadOnlyCollection<LdapAttribute> userAttributes)
    {
        ArgumentNullException.ThrowIfNull(userGroups);
        ArgumentNullException.ThrowIfNull(replyAttributes);
        ArgumentNullException.ThrowIfNull(userAttributes);
        
        UserName = userName;
        UserGroups = userGroups;
        ReplyAttributes = replyAttributes;
        Attributes = userAttributes;
    }
    
    public bool HasAttribute(string attributeName)
    {
        var attribute = Attributes.FirstOrDefault(x => x.Name.Value.ToLower(CultureInfo.InvariantCulture) == attributeName.ToLower(CultureInfo.InvariantCulture));
        return attribute is not null;
    }
    
    public string[] GetAttributeValues(string attributeName)
    {
        var attribute = Attributes.FirstOrDefault(x => x.Name.Value.ToLower(CultureInfo.InvariantCulture) == attributeName.ToLower(CultureInfo.InvariantCulture));
        return attribute?.GetNotEmptyValues() ?? [];
    }
}