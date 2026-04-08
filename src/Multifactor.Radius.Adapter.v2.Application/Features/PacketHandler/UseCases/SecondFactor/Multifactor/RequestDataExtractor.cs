using System.Net;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SecondFactor.Multifactor;

public static class RequestDataExtractor
{
    public static PersonalData ExtractPersonalData(RadiusPipelineContext context)
    {
        
        var callingStationIdAttributeName = context.ClientConfiguration.CallingStationIdAttribute;
        var callingStationIdAttribute = context.RequestPacket.GetCallingStationIdAttribute(callingStationIdAttributeName);
        var identity = GetSecondFactorIdentity(context);
        var callingStationId = GetCallingStationId(callingStationIdAttribute,
            context.RequestPacket.RemoteEndpoint, context.ClientConfiguration.IsIpFromUdp);

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
            : context.LdapProfile?.Attributes?.Where(attr => string.Equals(attr.Name, context.LdapConfiguration.IdentityAttribute, StringComparison.CurrentCultureIgnoreCase))
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

    public static string? GetCallingStationId(string? callingStationIdAttributeValue, IPEndPoint remoteEndPoint, bool isIpFromUdp)
    {
        if (!isIpFromUdp && !string.IsNullOrWhiteSpace(callingStationIdAttributeValue))
            return callingStationIdAttributeValue;
        
        return IPAddress.TryParse(callingStationIdAttributeValue, out _) 
            ? callingStationIdAttributeValue : remoteEndPoint.Address.ToString();
    }
}
