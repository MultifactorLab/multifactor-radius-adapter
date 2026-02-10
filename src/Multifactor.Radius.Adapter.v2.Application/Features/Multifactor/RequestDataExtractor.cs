using System.Net;
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
        return string.IsNullOrWhiteSpace(context.LdapConfiguration?.IdentityAttribute) ? context.RequestPacket.UserName 
            : context.LdapProfile?.Attributes?.Where(attr => attr.Name == context.LdapConfiguration.IdentityAttribute)
            .SelectMany(attr => attr.Values)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }

    public static string? GetUserPhone(RadiusPipelineContext context)
    {
        if (context.LdapProfile?.Attributes == null || 
            context.LdapConfiguration?.PhoneAttributes == null)
            return context.LdapProfile?.Phone;

        foreach (var attribute in context.LdapProfile.Attributes)
        {
            if (!context.LdapConfiguration.PhoneAttributes.Contains(attribute.Name.Value)) continue;
            foreach (var value in attribute.GetNotEmptyValues())
            {
                if (!string.IsNullOrEmpty(value))
                    return value;
            }
        }
        return context.LdapProfile.Phone;
    }

    public static string? GetCallingStationId(string? callingStationIdAttributeValue, IPEndPoint remoteEndPoint)
    {
        return IPAddress.TryParse(callingStationIdAttributeValue ?? string.Empty, out _)
            ? callingStationIdAttributeValue
            : remoteEndPoint.Address.ToString();
    }
}
