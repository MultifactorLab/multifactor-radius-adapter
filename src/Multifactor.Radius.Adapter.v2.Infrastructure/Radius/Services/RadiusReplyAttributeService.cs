// Infrastructure/Radius/Services/RadiusReplyAttributeService.cs

using System.Net;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Configuration.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Services;
using Multifactor.Radius.Adapter.v2.Shared.Extensions;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Radius.Services;

public class RadiusReplyAttributeService : IRadiusReplyAttributeService
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
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        
        var result = new Dictionary<string, List<object>>();
        
        foreach (var attribute in request.ReplyAttributes)
        {
            var values = ProcessAttribute(attribute.Key, attribute.Value, request);
            if (values.Any())
            {
                result[attribute.Key] = values;
            }
            
            // Если атрибут достаточный - прекращаем обработку
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
        RadiusReplyAttribute[] attributeValues,
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
    
    private bool ShouldIncludeAttribute(RadiusReplyAttribute attributeValue, GetReplyAttributesRequest request)
    {
        // 1. Проверка LDAP атрибутов
        if (attributeValue.FromLdap)
        {
            if (attributeValue.IsMemberOf)
                return request.UserGroups?.Count > 0;
            
            return !string.IsNullOrEmpty(attributeValue.Name) &&
                   request.HasAttribute(attributeValue.Name);
        }
        
        // 2. Проверка условий по имени пользователя
        if (attributeValue.UserNameCondition.Count > 0)
        {
            return MatchesUserNameCondition(attributeValue.UserNameCondition, request.UserName);
        }
        
        // 3. Проверка условий по группам
        if (attributeValue.UserGroupCondition.Count > 0)
        {
            return MatchesUserGroupCondition(attributeValue.UserGroupCondition, request.UserGroups);
        }
        
        // 4. Без условий - всегда включаем
        return true;
    }
    
    private static bool MatchesUserNameCondition(IReadOnlyList<string> conditions, string? userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
            return false;
            
        var canonicalUserName = userName.CanonicalizeUserName();
        
        foreach (var condition in conditions)
        {
            var nameToMatch = condition.IsCanonicalUserName()
                ? canonicalUserName 
                : userName;
            
            if (string.Equals(nameToMatch, condition, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        
        return false;
    }
    
    private static bool MatchesUserGroupCondition(IReadOnlyList<string> conditions, HashSet<string> userGroups)
    {
        if (userGroups == null || userGroups.Count == 0)
            return false;
            
        return conditions
            .Any(condition => userGroups
                .Any(group => string.Equals(group, condition, StringComparison.OrdinalIgnoreCase)));
    }
    
    private static List<object?> GetAttributeValues(RadiusReplyAttribute attributeValue, GetReplyAttributesRequest request)
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
    
    private static bool IsSufficientAttribute(RadiusReplyAttribute[] attributeValues)
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
        if (value is IPAddress ip)
            return ip.ToString();
        if (value is DateTime dt)
            return dt.ToString("O");
        if (value is string str && str.Length > 50)
            return $"{str[..50]}...";
            
        return value.ToString() ?? "null";
    }
}