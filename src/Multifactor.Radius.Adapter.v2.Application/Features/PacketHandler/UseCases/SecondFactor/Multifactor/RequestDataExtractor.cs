using System.Net;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SecondFactor.Multifactor;

public static class RequestDataExtractor
{
    public static PersonalData ExtractPersonalData(RadiusPipelineContext context)
    {
        var callingStationIdAttribute = context.ClientConfiguration.IsIpFromUdp
            ? context.RequestPacket.CallingStationIdAttribute
            : context.ClientConfiguration.CallingStationIdAttribute;
        var identity = GetSecondFactorIdentity(context);
        var callingStationId = GetCallingStationId(callingStationIdAttribute,
            context.RequestPacket.RemoteEndpoint);

        return new PersonalData
        {
            Identity = identity,
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

    private static string? GetUserPhone(RadiusPipelineContext context)
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
