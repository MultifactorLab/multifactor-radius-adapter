using System.Net;
using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap.LangFeatures;
using Multifactor.Radius.Adapter.v2.Domain;
using Multifactor.Radius.Adapter.v2.Domain.Auth;
using Multifactor.Radius.Adapter.v2.Domain.Radius.Packet;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;
using Multifactor.Radius.Adapter.v2.Infrastructure.Radius.Interfaces;

namespace Multifactor.Radius.Adapter.v2.Application.FirstFactor;

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

    public async Task ProcessFirstFactor(RadiusPipelineExecutionContext context)
    {
        Throw.IfNull(context, nameof(context));

        var requestPacket = context.RequestPacket;
        Throw.IfNull(requestPacket, nameof(requestPacket));
        
        if (string.IsNullOrWhiteSpace(requestPacket.UserName))
        {
            _logger.LogWarning("Can't find User-Name in message id={id} from {host:l}:{port}", context.RequestPacket.Identifier, context.RemoteEndpoint.Address, context.RemoteEndpoint.Port);
            context.AuthenticationState.FirstFactorStatus = GetAuthState(PacketCode.AccessReject);
            return;
        }

        try
        {
            var transformedName = UserNameTransformation.Transform(requestPacket.UserName, context.UserNameTransformRules.BeforeFirstFactor);
            var authPacket = PreparePacket(requestPacket, transformedName, context.Passphrase);

            var authBytes = _radiusPacketService.GetBytes(authPacket, context.RadiusSharedSecret);
            
            byte[]? response = null;
            IPEndPoint? endPoint = null;
            using var client = _radiusClientFactory.Create(context.ServiceClientEndpoint);
            foreach (var npsEndPoint in context.NpsServerEndpoints)
            {
                response = await SendRequestToNpsServer(client, npsEndPoint, authPacket.Identifier, requestPacket.Identifier, authBytes, context.NpsServerTimeout);
                if (response is not null)
                {
                    endPoint = npsEndPoint;
                    break;
                }

                _logger.LogWarning("Remote Radius Server did not respond on message with id={id}", authPacket.Identifier);
            }

            if (response is null)
            {
                context.AuthenticationState.FirstFactorStatus = GetAuthState(PacketCode.AccessReject);
                return;
            }

            var responsePacket = _radiusPacketService.Parse(response, context.RadiusSharedSecret, authPacket.Authenticator);
            _logger.LogDebug("Received {code:l} message with id={id} from Remote Radius Server", authPacket.Code.ToString(), authPacket.Identifier);

            if (responsePacket.Code == PacketCode.AccessAccept)
            {
                var userName = requestPacket.UserName;
                _logger.LogInformation("User '{user:l}' credential and status verified successfully at {endpoint:l}", userName, endPoint);
            }

            context.ResponsePacket = responsePacket;
            context.AuthenticationState.FirstFactorStatus = GetAuthState(responsePacket.Code);
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Radius authentication error");
        }

        context.AuthenticationState.FirstFactorStatus = GetAuthState(PacketCode.AccessReject);
    }

    private async Task<byte[]?> SendRequestToNpsServer(IRadiusClient client, IPEndPoint npsServerEndpoint, byte authIdentifier, byte requestIdentifier, byte[] payload, TimeSpan timeout)
    {
        _logger.LogDebug("Sending AccessRequest message with id={id} to Remote Radius Server {endpoint:l}", requestIdentifier, npsServerEndpoint);
        return await client.SendPacketAsync(authIdentifier, payload, npsServerEndpoint, timeout);
    }

    private static IRadiusPacket PreparePacket(IRadiusPacket radiusPacket, string userName, UserPassphrase passphrase)
    {
        var authPacket = new RadiusPacket(new RadiusPacketHeader(radiusPacket.Code, radiusPacket.Identifier, radiusPacket.Authenticator));

        foreach (var attr in radiusPacket.Attributes.Values)
        {
            foreach (var attrValue in attr.Values)
                authPacket.AddAttributeValue(attr.Name, attrValue);
        }

        authPacket.RemoveAttribute("Proxy-State");
        // authPacket.RemoveAttribute("State"); MF NPS does not send a response with State, but it should be
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