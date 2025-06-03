using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap.LangFeatures;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.Auth.PreAuthMode;
using Multifactor.Radius.Adapter.v2.Core.Radius.Packet;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;
using Multifactor.Radius.Adapter.v2.Services.Radius;

namespace Multifactor.Radius.Adapter.v2.Core.FirstFactor;

public class RadiusFirstFactorProcessor : IFirstFactorProcessor
{
    private readonly IRadiusPacketService _radiusPacketService;
    private readonly ILogger<RadiusFirstFactorProcessor> _logger;

    public AuthenticationSource AuthenticationSource => AuthenticationSource.Radius;

    public RadiusFirstFactorProcessor(IRadiusPacketService radiusPacketService, ILogger<RadiusFirstFactorProcessor> logger)
    {
        Throw.IfNull(radiusPacketService, nameof(radiusPacketService));
        Throw.IfNull(logger, nameof(logger));

        _radiusPacketService = radiusPacketService;
        _logger = logger;
    }

    public async Task ProcessFirstFactor(IRadiusPipelineExecutionContext context)
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
            var transformedName = UserNameTransformation.Transform(requestPacket.UserName, context.Settings.UserNameTransformRules.BeforeFirstFactor);
            var authPacket = PreparePacket(requestPacket, transformedName, context.Settings.PreAuthnMode);

            var authBytes = _radiusPacketService.GetBytes(authPacket, context.Settings.RadiusSharedSecret);
            using var client = new RadiusClient(context.Settings.ServiceClientEndpoint, _logger);
            _logger.LogDebug("Sending AccessRequest message with id={id} to Remote Radius Server {endpoint:l}", requestPacket.Identifier, context.Settings.NpsServerEndpoint);
            var response = await client.SendPacketAsync(authPacket.Identifier, authBytes, context.Settings.NpsServerEndpoint, TimeSpan.FromSeconds(5));

            if (response is null)
            {
                _logger.LogWarning("Remote Radius Server did not respond on message with id={id}", authPacket.Identifier);
                context.ExecutionState.SkipResponse();
                context.AuthenticationState.FirstFactorStatus = GetAuthState(PacketCode.AccessReject);
                return;
            }

            var responsePacket = _radiusPacketService.Parse(response, context.Settings.RadiusSharedSecret, authPacket.Authenticator);
            _logger.LogDebug("Received {code:l} message with id={id} from Remote Radius Server", authPacket.Code.ToString(), authPacket.Identifier);

            if (responsePacket.Code == PacketCode.AccessAccept)
            {
                var userName = context.RequestPacket.UserName;
                _logger.LogInformation("User '{user:l}' credential and status verified successfully at {endpoint:l}", userName, context.Settings.NpsServerEndpoint);
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

    private IRadiusPacket PreparePacket(IRadiusPacket radiusPacket, string userName, PreAuthModeDescriptor authModeDescriptor)
    {
        var authPacket = new RadiusPacket(new RadiusPacketHeader(radiusPacket.Code, radiusPacket.Identifier, radiusPacket.Authenticator));

        foreach (var attr in radiusPacket.Attributes.Values)
        {
            foreach (var attrValue in attr.Values)
                authPacket.AddAttributeValue(attr.Name, attrValue);
        }

        authPacket.RemoveAttribute("Proxy-State");
        authPacket.ReplaceAttribute("User-Name", userName);

        var pwd = radiusPacket.TryGetUserPassword();
        if (!string.IsNullOrWhiteSpace(pwd))
        {
            var userPassphrase = UserPassphrase.Parse(pwd, authModeDescriptor);
            if (!string.IsNullOrWhiteSpace(userPassphrase.Password))
                authPacket.ReplaceAttribute("User-Password", userPassphrase.Password);
        }

        return authPacket;
    }

    private AuthenticationStatus GetAuthState(PacketCode responseCode) => responseCode switch
    {
        PacketCode.AccessAccept => AuthenticationStatus.Accept,
        _ => AuthenticationStatus.Reject
    };
}