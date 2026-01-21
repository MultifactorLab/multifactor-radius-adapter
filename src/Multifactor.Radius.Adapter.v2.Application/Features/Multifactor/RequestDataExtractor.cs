using System.Net;
using Multifactor.Core.Ldap.Attributes;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.AccessChallenge.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Multifactor;

public static class RequestDataExtractor
{
    public static PersonalData ExtractPersonalData(RadiusPipelineContext context)
    {
        var identity = GetSecondFactorIdentity(context);
        var callingStationId = GetCallingStationId(
            context.RequestPacket.CallingStationIdAttribute,
            context.RequestPacket.RemoteEndpoint);

        return new PersonalData
        {
            Identity = identity ?? string.Empty,
            DisplayName = context.LdapProfile?.DisplayName,
            Email = context.LdapProfile?.Email,
            Phone = GetUserPhone(context),
            CalledStationId = context.RequestPacket.CalledStationIdAttribute,
            CallingStationId = callingStationId ?? string.Empty
        };
    }

    public static string? GetSecondFactorIdentity(RadiusPipelineContext context)
    {
        if (string.IsNullOrWhiteSpace(context.LdapConfiguration?.IdentityAttribute))
            return context.RequestPacket.UserName;

        return GetAttributeValue(context.LdapProfile?.Attributes, context.LdapConfiguration.IdentityAttribute);
    }

    public static string? GetUserPhone(RadiusPipelineContext context)
    {
        if (context.LdapProfile?.Attributes == null || 
            context.LdapConfiguration?.PhoneAttributes == null)
            return context.LdapProfile?.Phone;

        foreach (var attribute in context.LdapProfile.Attributes)
        {
            if (context.LdapConfiguration.PhoneAttributes.Contains(attribute.Name.Value))
            {
                foreach (var value in attribute.GetNotEmptyValues())
                {
                    if (!string.IsNullOrEmpty(value))
                        return value;
                }
            }
        }

        return context.LdapProfile.Phone;
    }

    private static string? GetAttributeValue(IReadOnlyCollection<LdapAttribute>? attributes, string attributeName)
    {
        if (attributes == null) return null;

        foreach (var attr in attributes)
        {
            if (attr.Name == attributeName)
            {
                foreach (var value in attr.Values)
                {
                    if (!string.IsNullOrWhiteSpace(value))
                        return value;
                }
            }
        }

        return null;
    }

    public static string? GetCallingStationId(string? callingStationIdAttributeValue, IPEndPoint remoteEndPoint)
    {
        return IPAddress.TryParse(callingStationIdAttributeValue ?? string.Empty, out _)
            ? callingStationIdAttributeValue
            : remoteEndPoint.Address.ToString();
    }
}
