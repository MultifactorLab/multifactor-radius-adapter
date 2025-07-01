using System.Text;
using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap.LangFeatures;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.Pipeline;
using Multifactor.Radius.Adapter.v2.Core.Radius.Packet;
using Multifactor.Radius.Adapter.v2.Server.Udp;
using Multifactor.Radius.Adapter.v2.Services.Radius;

namespace Multifactor.Radius.Adapter.v2.Services.AdapterResponseSender;

public class AdapterResponseSender : IResponseSender
{
    private readonly IRadiusPacketService _radiusPacketService;
    private readonly IRadiusReplyAttributeService _radiusReplyAttributeService;
    private readonly IUdpClient _udpClient;
    private readonly ILogger<AdapterResponseSender>  _logger;
    public AdapterResponseSender(
        IRadiusPacketService radiusPacketService,
        IUdpClient udpClient,
        IRadiusReplyAttributeService radiusReplyAttributeService,
        ILogger<AdapterResponseSender> logger)
    {
        Throw.IfNull(radiusPacketService, nameof(radiusPacketService));
        Throw.IfNull(udpClient, nameof(udpClient));
        
        _radiusPacketService = radiusPacketService;
        _radiusReplyAttributeService = radiusReplyAttributeService;
        _udpClient = udpClient;
        _logger = logger;
    }
    
    public async Task SendResponse(SendAdapterResponseRequest request)
    {
        if (request.ShouldSkipResponse)
            return;
        
        if (request.ResponsePacket?.IsEapMessageChallenge == true)
        {
            //EAP authentication in process, just proxy response
            _logger.LogDebug("Proxying EAP-Message Challenge to {host:l}:{port} id={id}", request.RemoteEndpoint.Address, request.RemoteEndpoint.Port, request.RequestPacket.Identifier);
            await SendResponse(request.ResponsePacket, request);
            return;
        }
        
        if (request.RequestPacket.IsVendorAclRequest && request.ResponsePacket != null)
        {
            //ACL and other rules transfer, just proxy response
            _logger.LogDebug("Proxying #ACSACL# to {host:l}:{port} id={id}", request.RemoteEndpoint.Address, request.RemoteEndpoint.Port, request.RequestPacket.Identifier);
            await SendResponse(request.ResponsePacket, request);
            return;
        }
        
        var requestPacket = request.RequestPacket;
        var responsePacketCode = ToPacketCode(request.AuthenticationState);
        var responsePacket = _radiusPacketService.CreateResponsePacket(requestPacket, responsePacketCode);

        switch (responsePacketCode)
        {
            case PacketCode.AccessAccept:
                AddResponsePacketAttributes(request.ResponsePacket, responsePacket);
                AddReplyAttributes(responsePacket, request);
                break;
            case PacketCode.AccessReject:
                if (request.ResponsePacket != null && request.ResponsePacket.Code == PacketCode.AccessReject)
                    AddResponsePacketAttributes(request.ResponsePacket, responsePacket);
                break;
            case PacketCode.AccessChallenge:
                if (!string.IsNullOrWhiteSpace(request.ResponseInformation.ReplyMessage))
                    responsePacket.ReplaceAttribute("Reply-Message", request.ResponseInformation.ReplyMessage);
        
                if (!string.IsNullOrWhiteSpace(request.ResponseInformation.State))
                    responsePacket.ReplaceAttribute("State", request.ResponseInformation.State);
                break;
            default:
                throw new NotImplementedException(responsePacketCode.ToString());
        }
        
        AddProxyAttribute(requestPacket, responsePacket);
        
        AddMessageAuthenticator(responsePacket);
        
        await SendResponse(responsePacket, request);
        var endpoint = request.ProxyEndpoint ?? request.RemoteEndpoint;
        _logger.LogInformation("{code:l} sent to {host:l}:{port} id={id} user='{user:l}'", responsePacket.Code.ToString(), endpoint.Address, endpoint.Port, responsePacket.Identifier, request.RequestPacket.UserName);
    }

    private void AddResponsePacketAttributes(IRadiusPacket? source, RadiusPacket target)
    {
        if (source is null)
            return;
        foreach (var attribute in source.Attributes.Values)
        {
            target.RemoveAttribute(attribute.Name);
            foreach (var value in attribute.Values)
                target.AddAttributeValue(attribute.Name, value);
        }
    }

    private void AddProxyAttribute(IRadiusPacket source, RadiusPacket target)
    {
        if (!source.Attributes.ContainsKey("Proxy-State"))
            return;
        if (!target.Attributes.ContainsKey("Proxy-State"))
            target.ReplaceAttribute("Proxy-State", source.Attributes.SingleOrDefault(o => o.Key == "Proxy-State").Value.Values.Single());
    }

    private void AddMessageAuthenticator(RadiusPacket target)
    {
        if (target.Attributes.ContainsKey("Message-Authenticator"))
            return;
        
        var placeholder = new byte[16];
        var placeholderStr = Encoding.Default.GetString(placeholder);
        target.AddAttributeValue("Message-Authenticator", placeholderStr);
    }

    private void AddReplyAttributes(RadiusPacket target, SendAdapterResponseRequest request)
    {
        var replyAttributesRequest = new GetReplyAttributesRequest(
            request.RequestPacket.UserName!,
            request.UserGroups,
            request.RadiusReplyAttributes,
            request.Attributes);
        
       var attributes = _radiusReplyAttributeService.GetReplyAttributes(replyAttributesRequest);
       foreach (var attribute in attributes)
       {
           target.RemoveAttribute(attribute.Key);
           foreach (var attrValue in attribute.Value)
               target.AddAttributeValue(attribute.Key, attrValue);
       }
    }

    private PacketCode ToPacketCode(IAuthenticationState authenticationState)
    {
        var successfulFirstFactor = authenticationState.FirstFactorStatus is AuthenticationStatus.Accept or AuthenticationStatus.Bypass;
        var successfulSecondFactor = authenticationState.SecondFactorStatus is AuthenticationStatus.Accept or AuthenticationStatus.Bypass;
        if (successfulFirstFactor && successfulSecondFactor)
            return PacketCode.AccessAccept;
        var authFailed = authenticationState.FirstFactorStatus == AuthenticationStatus.Reject || authenticationState.SecondFactorStatus == AuthenticationStatus.Reject;
        return authFailed ? PacketCode.AccessReject : PacketCode.AccessChallenge;
    }
    
    private async Task SendResponse(IRadiusPacket responsePacket, SendAdapterResponseRequest context)
    {
        var bytes = _radiusPacketService.GetBytes(responsePacket, context.RadiusSharedSecret);
        var endpoint = context.ProxyEndpoint ?? context.RemoteEndpoint;
        
        await _udpClient.SendAsync(bytes, bytes.Length, endpoint);
    }
}