using System.Net;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Abstractions;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.Adapters.Radius;

public interface IRadiusReplyAttributeService
{
    IDictionary<string, List<object>> GetReplyAttributes(GetReplyAttributesRequest request);
}

internal sealed class RadiusReplyAttributeService : IRadiusReplyAttributeService
{
    private readonly IRadiusAttributeTypeConverter _typeConverter;
    private readonly ILogger<RadiusReplyAttributeService> _logger;
    
    public RadiusReplyAttributeService(
        IRadiusAttributeTypeConverter typeConverter,
        ILogger<RadiusReplyAttributeService> logger)
    {
        _typeConverter = typeConverter ?? throw new ArgumentNullException(nameof(typeConverter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public IDictionary<string, List<object>> GetReplyAttributes(GetReplyAttributesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.ReplyAttributes, nameof(request.ReplyAttributes));
        
        var result = new Dictionary<string, List<object>>();
        
        foreach (var attribute in request.ReplyAttributes)
        {
            var values = ProcessAttribute(attribute.Key, attribute.Value, request);
            if (values.Count != 0)
            {
                result[attribute.Key] = values;
            }
            
            if (IsSufficientAttribute(attribute.Value))
            {
                _logger.LogDebug("Sufficient attribute '{Attribute}' found, stopping processing", attribute.Key);
                break;
            }
        }
        
        LogResult(result);
        return result;
    }
    
    private List<object> ProcessAttribute(
        string attributeName,
        IReadOnlyList<IRadiusReplyAttribute> attributeValues,
        GetReplyAttributesRequest request)
    {
        var result = new List<object>();
        
        foreach (var attributeValue in attributeValues)
        {
            if (!ShouldIncludeAttribute(attributeValue, request))
                continue;
            
            var values = GetAttributeValues(attributeValue, request);
            foreach (var value in values)
            {
                if (value is null)
                {
                    _logger.LogDebug("Skipping null value for attribute '{Attribute}'", attributeName);
                    continue;
                }
                
                var convertedValue = _typeConverter.ConvertType(attributeName, value);
                result.Add(convertedValue);
                
                _logger.LogDebug(
                    "Added attribute '{Attribute}': {Value}",
                    attributeName,
                    GetLoggableValue(convertedValue));
            }
            
            if (attributeValue.Sufficient)
                break;
        }
        
        return result;
    }
    
    private static bool ShouldIncludeAttribute(IRadiusReplyAttribute attributeValue, GetReplyAttributesRequest request)
    {
        if (attributeValue.FromLdap)
        {
            if (attributeValue.IsMemberOf)
                return request.UserGroups?.Count > 0;
            
            return !string.IsNullOrEmpty(attributeValue.Name) &&
                   request.HasAttribute(attributeValue.Name);
        }
        
        if (attributeValue.UserNameCondition.Count > 0)
        {
            return MatchesUserNameCondition(attributeValue.UserNameCondition, request.UserName);
        }
        
        if (attributeValue.UserGroupCondition.Count > 0)
        {
            return MatchesUserGroupCondition(attributeValue.UserGroupCondition, request.UserGroups);
        }
        
        return true;
    }
    
    private static bool MatchesUserNameCondition(IReadOnlyList<string> conditions, string? userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
            return false;
            
        var canonicalUserName = CanonicalizeUserName(userName);
        
        foreach (var condition in conditions)
        {
            var nameToMatch = IsCanonicalUserName(condition)
                ? canonicalUserName 
                : userName;
            
            if (string.Equals(nameToMatch, condition, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        
        return false;
    }
    
    private static bool MatchesUserGroupCondition(IReadOnlyList<string> conditions, HashSet<string>? userGroups)
    {
        if (userGroups == null || userGroups.Count == 0)
            return false;
            
        return conditions
            .Any(condition => userGroups
                .Any(group => string.Equals(group, condition, StringComparison.OrdinalIgnoreCase)));
    }
    
    private static List<object?> GetAttributeValues(IRadiusReplyAttribute attributeValue, GetReplyAttributesRequest request)
    {
        if (attributeValue.IsMemberOf)
        {
            return request.UserGroups
                .Select(group => (object?)group)
                .ToList();
        }
        
        if (attributeValue.FromLdap && !string.IsNullOrEmpty(attributeValue.Name))
        {
            return request.GetAttributeValues(attributeValue.Name)
                .Select(value => (object?)value)
                .ToList();
        }
        
        return [attributeValue.Value];
    }
    
    private static bool IsSufficientAttribute(IReadOnlyList<IRadiusReplyAttribute> attributeValues)
    {
        return attributeValues.Any(av => av.Sufficient);
    }
    
    private void LogResult(IDictionary<string, List<object>> result)
    {
        if (!_logger.IsEnabled(LogLevel.Debug))
            return;
            
        var attributeCount = result.Sum(kvp => kvp.Value.Count);
        _logger.LogDebug(
            "Generated {AttributeCount} reply attribute values in {GroupCount} groups",
            attributeCount,
            result.Count);
    }
    
    private static string GetLoggableValue(object value)
    {
        return value switch
        {
            IPAddress ip => ip.ToString(),
            DateTime dt => dt.ToString("O"),
            string str when str.Length > 50 => $"{str[..50]}...",
            _ => value.ToString() ?? "null"
        };
    }

    private static bool IsCanonicalUserName(string userName)
    {
        if (string.IsNullOrEmpty(userName))
        {
            throw new ArgumentNullException(nameof(userName));
        }
        return userName.IndexOfAny(['\\', '@']) == -1;
    }
    
    private static string CanonicalizeUserName(string userName)
    {
        if (string.IsNullOrEmpty(userName))
        {
            throw new ArgumentNullException(nameof(userName));
        }

        var identity = userName.ToLower();
        var index = identity.IndexOf('\\', StringComparison.Ordinal);
        if (index > 0)
        {
            identity = identity[(index + 1)..];
        }

        index = identity.IndexOf('@', StringComparison.Ordinal);
        if (index > 0)
        {
            identity = identity[..index];
        }

        return identity;
    }
}