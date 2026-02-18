using System.Net;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Core;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Enum;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models.Enum;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models.Enums;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Ports;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.FirstFactor;

public class RadiusFirstFactorProcessor : IFirstFactorProcessor
{
    private readonly IRadiusPacketService _radiusPacketService;
    private readonly IRadiusClientFactory _radiusClientFactory;
    private readonly ILogger<RadiusFirstFactorProcessor> _logger;

    public AuthenticationSource AuthenticationSource => AuthenticationSource.Radius;

    public RadiusFirstFactorProcessor(IRadiusPacketService radiusPacketService, IRadiusClientFactory radiusClientFactory, ILogger<RadiusFirstFactorProcessor> logger)
    {
        _radiusPacketService = radiusPacketService;
        _radiusClientFactory = radiusClientFactory;
        _logger = logger;
    }

    public async Task ProcessFirstFactor(RadiusPipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        var requestPacket = context.RequestPacket;
        ArgumentNullException.ThrowIfNull(requestPacket, nameof(requestPacket));
        
        if (string.IsNullOrWhiteSpace(requestPacket.UserName))
        {
            _logger.LogWarning("Can't find User-Name in message id={id} from {host:l}:{port}", context.RequestPacket.Identifier, context.RequestPacket.RemoteEndpoint.Address, context.RequestPacket.RemoteEndpoint.Port);
            context.FirstFactorStatus = GetAuthState(PacketCode.AccessReject);
            return;
        }

        try
        {
            var transformedName = requestPacket.UserName;
            var authPacket = PreparePacket(requestPacket, transformedName, context.Passphrase);

            var authBytes = _radiusPacketService.SerializePacket(authPacket, new SharedSecret(context.ClientConfiguration.RadiusSharedSecret));
            
            byte[]? response = null;
            IPEndPoint? endPoint = null;
            using var client = _radiusClientFactory.CreateRadiusClient(context.ClientConfiguration.AdapterClientEndpoint);
            foreach (var npsEndPoint in context.ClientConfiguration.NpsServerEndpoints)
            {
                response = await SendRequestToNpsServer(client, npsEndPoint, authPacket.Identifier, requestPacket.Identifier, authBytes, context.ClientConfiguration.NpsServerTimeout);
                if (response is not null)
                {
                    endPoint = npsEndPoint;
                    break;
                }

                _logger.LogWarning("Remote Radius Server did not respond on message with id={id}", authPacket.Identifier);
            }

            if (response is null)
            {
                context.FirstFactorStatus = GetAuthState(PacketCode.AccessReject);
                return;
            }

            var responsePacket = _radiusPacketService.ParsePacket(response, new SharedSecret(context.ClientConfiguration.RadiusSharedSecret), authPacket.Authenticator);
            _logger.LogDebug("Received {code:l} message with id={id} from Remote Radius Server", authPacket.Code.ToString(), authPacket.Identifier);

            if (responsePacket.Code == PacketCode.AccessAccept)
            {
                var userName = requestPacket.UserName;
                _logger.LogInformation("User '{user:l}' credential and status verified successfully at {endpoint:l}", userName, endPoint);
            }

            context.ResponsePacket = responsePacket;
            context.FirstFactorStatus = GetAuthState(responsePacket.Code);
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Radius authentication error");
        }

        context.FirstFactorStatus = GetAuthState(PacketCode.AccessReject);
    }

    private async Task<byte[]?> SendRequestToNpsServer(IRadiusClient client, IPEndPoint npsServerEndpoint, byte authIdentifier, byte requestIdentifier, byte[] payload, TimeSpan timeout)
    {
        _logger.LogDebug("Sending AccessRequest message with id={id} to Remote Radius Server {endpoint:l}", requestIdentifier, npsServerEndpoint);
        return await client.SendPacketAsync(authIdentifier, payload, npsServerEndpoint, timeout);
    }

    private static RadiusPacket PreparePacket(RadiusPacket radiusPacket, string userName, UserPassphrase passphrase)
    {
        var authPacket = new RadiusPacket(new RadiusPacketHeader(radiusPacket.Code, radiusPacket.Identifier, radiusPacket.Authenticator));

        foreach (var attr in radiusPacket.Attributes.Values)
        {
            foreach (var attrValue in attr.Values)
                authPacket.AddAttributeValue(attr.Name, attrValue);
        }

        authPacket.RemoveAttribute("Proxy-State");
        authPacket.ReplaceAttribute("User-Name", userName);

        if (!string.IsNullOrWhiteSpace(passphrase.Password))
            authPacket.ReplaceAttribute("User-Password", passphrase.Password);

        return authPacket;
    }

    private static AuthenticationStatus GetAuthState(PacketCode responseCode) => responseCode switch
    {
        PacketCode.AccessAccept => AuthenticationStatus.Accept,
        _ => AuthenticationStatus.Reject
    };
}