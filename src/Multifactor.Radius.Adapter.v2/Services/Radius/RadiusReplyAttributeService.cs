using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Core;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;

namespace Multifactor.Radius.Adapter.v2.Services.Radius;

public class RadiusReplyAttributeService : IRadiusReplyAttributeService
{
    private readonly IRadiusAttributeTypeConverter _converter;
    private readonly ILogger<RadiusReplyAttributeService> _logger;

    public RadiusReplyAttributeService(IRadiusAttributeTypeConverter converter, ILogger<RadiusReplyAttributeService> logger)
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

            ProcessReplyAttributeValue(attr, request, convertedValues, out var breakLoop);
            
            attributes.Add(attr.Key, convertedValues);
            if (breakLoop)
                break;
        }
        
        return attributes;
    }

    private void ProcessReplyAttributeValue(KeyValuePair<string, RadiusReplyAttributeValue[]> attr, GetReplyAttributesRequest request, List<object> convertedValues, out bool breakLoop)
    {
        breakLoop = false;
        foreach (var attrElement in attr.Value)
        {
            if (!IsMatch(request, attrElement))
                continue;

            foreach (var val in GetValues(request, attrElement))
            {
                if (val is null)
                {
                    _logger.LogDebug("Attribute '{attrname:l}' got no value, skipping", attr.Key);
                    continue;
                }
                    
                _logger.LogDebug("Added/replaced attribute '{attrname:l}:{attrval:l}' to reply", attr.Key, val.ToString());
                convertedValues.Add(_converter.ConvertType(attr.Key, val));
            }

            if (!attrElement.Sufficient)
                continue;
                
            breakLoop = true;
            return;
        }
    }

    private bool IsMatch(GetReplyAttributesRequest request, RadiusReplyAttributeValue attributeValue)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (attributeValue.FromLdap)
        {
            if (attributeValue.IsMemberOf)
                return request.UserGroups?.Count > 0;
            
            return request.HasAttribute(attributeValue.LdapAttributeName);
        }

        if (attributeValue.UserNameCondition.Count != 0)
        {
            var userName = request.UserName;
            var canonicalUserName = Utils.CanonicalizeUserName(userName!);
            return attributeValue.UserNameCondition.Any(x => CompareUserName(x, userName, canonicalUserName));
        }

        if (attributeValue.UserGroupCondition.Count != 0)
            return attributeValue
                .UserGroupCondition
                .Intersect(request.UserGroups, StringComparer.OrdinalIgnoreCase)
                .Any();

        return true;
    }

    private object?[] GetValues(GetReplyAttributesRequest context, RadiusReplyAttributeValue attributeValue)
    {
        if (attributeValue.IsMemberOf)
            return context.UserGroups.Select(x => x as object).ToArray();

        if (!attributeValue.FromLdap)
            return [attributeValue.Value];
        
        var attrValue = context.GetAttributeValues(attributeValue.LdapAttributeName);
        return attrValue.Select(x => x as object).ToArray();
    }

    private bool CompareUserName(string conditionName, string userName, string canonicalUserName)
    {
        var toMatch = Utils.IsCanicalUserName(conditionName)
            ? canonicalUserName
            : userName;
                
        return string.Compare(toMatch, conditionName, StringComparison.InvariantCultureIgnoreCase) == 0;
    }
}