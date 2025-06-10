using System.Text;
using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap.LangFeatures;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.Pipeline;
using Multifactor.Radius.Adapter.v2.Core.Radius.Packet;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;
using Multifactor.Radius.Adapter.v2.Server.Udp;
using Multifactor.Radius.Adapter.v2.Services.Radius;

namespace Multifactor.Radius.Adapter.v2.Services;

public class AdapterResponseSender : IResponseSender
{
    private readonly IRadiusPacketService _radiusPacketService;
    private readonly IUdpClient _udpClient;
    private readonly ILogger<AdapterResponseSender>  _logger;
    public AdapterResponseSender(IRadiusPacketService radiusPacketService, IUdpClient udpClient, ILogger<AdapterResponseSender> logger)
    {
        Throw.IfNull(radiusPacketService, nameof(radiusPacketService));
        Throw.IfNull(udpClient, nameof(udpClient));
        
        _radiusPacketService = radiusPacketService;
        _udpClient = udpClient;
        _logger = logger;
    }
    
    public async Task SendResponse(IRadiusPipelineExecutionContext context)
    {
        if (context.ExecutionState.ShouldSkipResponse)
            return;
        
        var requestPacket = context.RequestPacket;
        var responsePacketCode = ToPacketCode(context.AuthenticationState);
        var responsePacket = _radiusPacketService.CreateResponsePacket(requestPacket, responsePacketCode);
        AddResponsePacketAttributes(context.ResponsePacket, responsePacket);
        AddProxyAttribute(requestPacket, responsePacket);
        AddMessageAuthenticator(responsePacket);
        AddReplyAttributes(responsePacket);
        responsePacket.ReplaceAttribute("Reply-Message", context.ResponseInformation.ReplyMessage ?? "This is MVP response. Congratulations!");
        
        var bytes = _radiusPacketService.GetBytes(responsePacket, context.Settings.RadiusSharedSecret);
        var endpoint = context.ProxyEndpoint ?? context.RemoteEndpoint;
        
        await _udpClient.SendAsync(bytes, bytes.Length, endpoint);
        _logger.LogInformation("{code:l} sent to {host:l}:{port} id={id} user='{user:l}'", responsePacket.Code.ToString(), endpoint.Address, endpoint.Port, responsePacket.Identifier, requestPacket.UserName);
    }

    private void AddResponsePacketAttributes(IRadiusPacket? source, RadiusPacket target)
    {
        if (source is null)
            return;
        foreach (var attribute in source.Attributes.Values)
        {
            foreach (var value in attribute.Values)
            {
                target.AddAttributeValue(attribute.Name, value);
            }
        }
    }

    private void AddProxyAttribute(IRadiusPacket source, RadiusPacket target)
    {
        if (source.Attributes.ContainsKey("Proxy-State"))
        {
            if (!target.Attributes.ContainsKey("Proxy-State"))
            {
                target.ReplaceAttribute("Proxy-State", source.Attributes.SingleOrDefault(o => o.Key == "Proxy-State").Value.Values.Single());
            }
        }
    }

    private void AddMessageAuthenticator(RadiusPacket target)
    {
        if (!target.Attributes.ContainsKey("Message-Authenticator"))
        {
            var placeholder = new byte[16];
            var placeholderStr = Encoding.Default.GetString(placeholder);
            target.AddAttributeValue("Message-Authenticator", placeholderStr);
        }
    }

    private void AddReplyAttributes(RadiusPacket target)
    {
        return;
    }

    private PacketCode ToPacketCode(IAuthenticationState authenticationState)
    {
        if ((authenticationState.FirstFactorStatus == AuthenticationStatus.Accept || authenticationState.FirstFactorStatus == AuthenticationStatus.Bypass)
            &&
            (authenticationState.SecondFactorStatus == AuthenticationStatus.Accept || authenticationState.SecondFactorStatus == AuthenticationStatus.Bypass))
        {
            return PacketCode.AccessAccept;
        }

        if (authenticationState.FirstFactorStatus == AuthenticationStatus.Reject || authenticationState.SecondFactorStatus == AuthenticationStatus.Reject)
        {
            return PacketCode.AccessReject;
        }

        return PacketCode.AccessChallenge;
    }
}