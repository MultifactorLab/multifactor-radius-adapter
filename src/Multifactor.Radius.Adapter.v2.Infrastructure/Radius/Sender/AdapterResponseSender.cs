using System.Text;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models.Enums;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Ports;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Services;
using Multifactor.Radius.Adapter.v2.Application.Models.Enum;
using Multifactor.Radius.Adapter.v2.Application.Ports;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Radius.Sender;

public class AdapterResponseSender : IResponseSender
{
    private readonly IRadiusPacketService _radiusPacketService;
    private readonly IRadiusReplyAttributeService _radiusReplyAttributeService;
    private readonly IUdpClient _udpClient;
    private readonly ILogger<AdapterResponseSender> _logger;
    
    // Константы
    private const string MessageAuthenticatorAttribute = "Message-Authenticator";
    private const string ProxyStateAttribute = "Proxy-State";
    private const string StateAttribute = "State";
    private const string ReplyMessageAttribute = "Reply-Message";
    
    public AdapterResponseSender(
        IRadiusPacketService radiusPacketService,
        IUdpClient udpClient,
        IRadiusReplyAttributeService radiusReplyAttributeService,
        ILogger<AdapterResponseSender> logger)
    {
        _radiusPacketService = radiusPacketService ?? throw new ArgumentNullException(nameof(radiusPacketService));
        _udpClient = udpClient ?? throw new ArgumentNullException(nameof(udpClient));
        _radiusReplyAttributeService = radiusReplyAttributeService ?? throw new ArgumentNullException(nameof(radiusReplyAttributeService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task SendResponse(SendAdapterResponseRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        
        if (request.ShouldSkipResponse)
        {
            _logger.LogDebug("Skipping response for request Id={Id}", request.RequestPacket?.Identifier);
            return;
        }
        
        // Проверка специальных случаев
        if (ShouldProxyResponse(request))
        {
            await ProxyResponseAsync(request);
            return;
        }
        
        // Построение и отправка обычного ответа
        var responsePacket = BuildResponsePacket(request);
        await SendResponsePacketAsync(responsePacket, request);
        
        LogResponseSent(responsePacket, request);
    }
    
    private bool ShouldProxyResponse(SendAdapterResponseRequest request)
    {
        // EAP challenge
        if (request.ResponsePacket?.IsEapMessageChallenge == true)
            return true;
            
        // Vendor ACL request
        if (request.RequestPacket.IsVendorAclRequest && request.ResponsePacket != null)
            return true;
            
        return false;
    }
    
    private async Task ProxyResponseAsync(SendAdapterResponseRequest request)
    {
        if (request.ResponsePacket == null)
            return;
            
        var logMessage = request.RequestPacket.IsVendorAclRequest 
            ? "Proxying #ACSACL#" 
            : "Proxying EAP-Message Challenge";
            
        _logger.LogDebug(
            "{Action} to {Host}:{Port} id={Id}", 
            logMessage, 
            request.RemoteEndpoint.Address, 
            request.RemoteEndpoint.Port, 
            request.RequestPacket.Identifier);
            
        await SendResponsePacketAsync(request.ResponsePacket, request);
    }
    
    private RadiusPacket BuildResponsePacket(SendAdapterResponseRequest request)
    {
        var responsePacketCode = DetermineResponseCode(request.FirstFactorStatus, request.SecondFactorStatus);
        var responsePacket = _radiusPacketService.CreateResponse(
            request.RequestPacket, 
            responsePacketCode);
        
        // Обработка в зависимости от типа ответа
        switch (responsePacketCode)
        {
            case PacketCode.AccessAccept:
                ProcessAccessAcceptResponse(responsePacket, request);
                break;
                
            case PacketCode.AccessReject:
                ProcessAccessRejectResponse(responsePacket, request);
                break;
                
            case PacketCode.AccessChallenge:
                ProcessAccessChallengeResponse(responsePacket, request);
                break;
                
            default:
                throw new NotSupportedException(
                    $"Response packet code {responsePacketCode} is not supported");
        }
        
        // Добавляем общие атрибуты
        AddCommonAttributes(responsePacket, request);
        
        return responsePacket;
    }
    
    private void ProcessAccessAcceptResponse(RadiusPacket responsePacket, SendAdapterResponseRequest request)
    {
        // Копируем атрибуты из исходного ответа (если есть)
        if (request.ResponsePacket != null)
        {
            CopyAttributes(request.ResponsePacket, responsePacket);
        }
        
        // Добавляем reply-атрибуты
        AddReplyAttributes(responsePacket, request);
    }
    
    private void ProcessAccessRejectResponse(RadiusPacket responsePacket, SendAdapterResponseRequest request)
    {
        // Для Reject копируем атрибуты только если это тоже Reject
        if (request.ResponsePacket?.Code == PacketCode.AccessReject)
        {
            CopyAttributes(request.ResponsePacket, responsePacket);
        }
    }
    
    private void ProcessAccessChallengeResponse(RadiusPacket responsePacket, SendAdapterResponseRequest request)
    {
        // Добавляем State атрибут если есть
        if (!string.IsNullOrWhiteSpace(request.ResponseInformation.State))
        {
            responsePacket.ReplaceAttribute(StateAttribute, request.ResponseInformation.State);
        }
    }
    
    private void AddCommonAttributes(RadiusPacket responsePacket, SendAdapterResponseRequest request)
    {
        // Reply-Message
        if (!string.IsNullOrWhiteSpace(request.ResponseInformation.ReplyMessage))
        {
            responsePacket.ReplaceAttribute(ReplyMessageAttribute, request.ResponseInformation.ReplyMessage);
        }
        
        // Proxy-State
        AddProxyStateAttribute(request.RequestPacket, responsePacket);
        
        // Message-Authenticator (placeholder если нет)
        AddMessageAuthenticatorIfMissing(responsePacket);
    }
    
    private void CopyAttributes(RadiusPacket source, RadiusPacket target)
    {
        if (source == null || target == null)
            return;
            
        foreach (var attribute in source.Attributes.Values)
        {
            // Удаляем старый атрибут если есть
            target.RemoveAttribute(attribute.Name);
            
            // Добавляем все значения
            foreach (var value in attribute.Values)
            {
                target.AddAttributeValue(attribute.Name, value);
            }
        }
    }
    
    private void AddProxyStateAttribute(RadiusPacket source, RadiusPacket target)
    {
        if (source.Attributes.TryGetValue(ProxyStateAttribute, out var proxyStateAttribute))
        {
            // Добавляем только если еще нет
            if (!target.Attributes.ContainsKey(ProxyStateAttribute))
            {
                var value = proxyStateAttribute.Values.FirstOrDefault();
                if (value != null)
                {
                    target.AddAttributeValue(ProxyStateAttribute, value);
                }
            }
        }
    }
    
    private void AddMessageAuthenticatorIfMissing(RadiusPacket packet)
    {
        if (!packet.Attributes.ContainsKey(MessageAuthenticatorAttribute))
        {
            var placeholder = new byte[16];
            var placeholderStr = Encoding.ASCII.GetString(placeholder);
            packet.AddAttributeValue(MessageAuthenticatorAttribute, placeholderStr);
        }
    }
    
    private void AddReplyAttributes(RadiusPacket target, SendAdapterResponseRequest request)
    {
        var replyAttributesRequest = new GetReplyAttributesRequest(
            request.RequestPacket.UserName,
            request.UserGroups,
            request.RadiusReplyAttributes,
            request.Attributes);
        
        var attributes = _radiusReplyAttributeService.GetReplyAttributes(replyAttributesRequest);
        
        foreach (var attribute in attributes)
        {
            // Удаляем старый атрибут
            target.RemoveAttribute(attribute.Key);
            
            // Добавляем все значения
            foreach (var attrValue in attribute.Value)
            {
                target.AddAttributeValue(attribute.Key, attrValue);
            }
        }
    }
    
    private PacketCode DetermineResponseCode(AuthenticationStatus firstFactorStatus, AuthenticationStatus secondFactorStatus)
    {
        var successfulFirstFactor = firstFactorStatus 
            is AuthenticationStatus.Accept 
            or AuthenticationStatus.Bypass;
            
        var successfulSecondFactor = secondFactorStatus 
            is AuthenticationStatus.Accept 
            or AuthenticationStatus.Bypass;
            
        if (successfulFirstFactor && successfulSecondFactor)
            return PacketCode.AccessAccept;
            
        var authFailed = firstFactorStatus == AuthenticationStatus.Reject 
                      || secondFactorStatus == AuthenticationStatus.Reject;
                      
        return authFailed ? PacketCode.AccessReject : PacketCode.AccessChallenge;
    }
    
    private async Task SendResponsePacketAsync(RadiusPacket responsePacket, SendAdapterResponseRequest request)
    {
        var bytes = _radiusPacketService.SerializePacket(responsePacket, request.RadiusSharedSecret);
        var endpoint = request.ProxyEndpoint ?? request.RemoteEndpoint;
        
        // Задержка для AccessReject (security feature)
        if (responsePacket.Code == PacketCode.AccessReject 
            && request.InvalidCredentialDelay.HasValue)
        {
            await WaitSomeTimeAsync(
                request.InvalidCredentialDelay.Value.min, 
                request.InvalidCredentialDelay.Value.max);
        }
        
        await _udpClient.SendAsync(bytes, bytes.Length, endpoint);
    }
    private void LogResponseSent(RadiusPacket responsePacket, SendAdapterResponseRequest request)
    {
        var endpoint = request.ProxyEndpoint ?? request.RemoteEndpoint;
        var userName = request.RequestPacket.UserName;
        
        if (!string.IsNullOrWhiteSpace(userName))
        {
            _logger.LogInformation(
                "{Code} sent to {Host}:{Port} id={Id} user='{User}'", 
                responsePacket.Code.ToString(), 
                endpoint.Address, 
                endpoint.Port, 
                responsePacket.Identifier, 
                userName);
        }
        else 
        {
            _logger.LogInformation(
                "{Code} sent to {Host}:{Port} id={Id}", 
                responsePacket.Code.ToString(), 
                endpoint.Address, 
                endpoint.Port, 
                responsePacket.Identifier);
        }
    }
    
    private static Task WaitSomeTimeAsync(int min, int max)
    {
        var correctedMax = min == max ? max : max + 1;
        var delay = new Random().Next(min, correctedMax);

        return Task.Delay(TimeSpan.FromSeconds(delay));
    }
}