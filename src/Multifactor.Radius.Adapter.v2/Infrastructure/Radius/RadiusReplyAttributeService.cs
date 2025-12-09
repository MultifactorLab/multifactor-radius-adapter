using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Domain;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Infrastructure.Radius.Dto;
using Multifactor.Radius.Adapter.v2.Infrastructure.Radius.Interfaces;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Radius;

public class RadiusReplyAttributeService : IRadiusReplyAttributeService
{
    private readonly IRadiusAttributeTypeConverter _converter;
    private readonly ILogger<RadiusReplyAttributeService> _logger;

    public RadiusReplyAttributeService(
        IRadiusAttributeTypeConverter converter, 
        ILogger<RadiusReplyAttributeService> logger)
    {
        _converter = converter;
        _logger = logger;
    }
    
    public IDictionary<string, List<object>> GetReplyAttributes(GetReplyAttributesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        
        var attributes = new Dictionary<string, List<object>>();
        
        foreach (var attr in request.ReplyAttributes)
        {
            var convertedValues = new List<object>();
            var breakLoop = false;

            foreach (var attrElement in attr.Value)
            {
                if (!IsMatch(request, attrElement))
                    continue;

                foreach (var val in GetValues(request, attrElement))
                {
                    if (val is null)
                    {
                        _logger.LogDebug("Attribute '{AttrName}' got no value, skipping", attr.Key);
                        continue;
                    }
                        
                    _logger.LogDebug("Added/replaced attribute '{AttrName}:{AttrValue}'", attr.Key, val);
                    convertedValues.Add(_converter.ConvertType(attr.Key, val));
                }

                if (attrElement.Sufficient)
                {
                    breakLoop = true;
                    break;
                }
            }

            if (convertedValues.Count > 0)
                attributes.Add(attr.Key, convertedValues);
                
            if (breakLoop)
                break;
        }
        
        return attributes;
    }

    private static bool IsMatch(GetReplyAttributesRequest request, RadiusReplyAttributeValue attributeValue)
    {
        if (attributeValue.FromLdap)
        {
            if (attributeValue.IsMemberOf)
                return request.UserGroups?.Count > 0;
            
            return request.HasAttribute(attributeValue.LdapAttributeName);
        }

        if (attributeValue.UserNameCondition.Count != 0)
        {
            var userName = string.IsNullOrWhiteSpace(request.UserName) ? string.Empty : request.UserName;
            var canonicalUserName = Utils.CanonicalizeUserName(userName);
            return attributeValue.UserNameCondition.Any(x => CompareUserName(x, userName, canonicalUserName));
        }

        if (attributeValue.UserGroupCondition.Count != 0)
            return attributeValue
                .UserGroupCondition
                .Intersect(request.UserGroups, StringComparer.OrdinalIgnoreCase)
                .Any();

        return true;
    }

    private static object?[] GetValues(GetReplyAttributesRequest request, RadiusReplyAttributeValue attributeValue)
    {
        if (attributeValue.IsMemberOf)
            return request.UserGroups.Select(x => x as object).ToArray();

        if (!attributeValue.FromLdap)
            return [attributeValue.Value];
        
        var attrValue = request.GetAttributeValues(attributeValue.LdapAttributeName);
        return attrValue.Select(x => x as object).ToArray();
    }

    private static bool CompareUserName(string conditionName, string userName, string canonicalUserName)
    {
        var toMatch = Utils.IsCanonicalUserName(conditionName)
            ? canonicalUserName
            : userName;
                
        return string.Compare(toMatch, conditionName, StringComparison.InvariantCultureIgnoreCase) == 0;
    }
}