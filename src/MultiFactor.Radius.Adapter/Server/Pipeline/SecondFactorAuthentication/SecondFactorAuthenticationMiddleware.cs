//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Core.Exceptions;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Framework.Context;
using MultiFactor.Radius.Adapter.Framework.Pipeline;
using MultiFactor.Radius.Adapter.Server.Pipeline.AccessChallenge;
using MultiFactor.Radius.Adapter.Services.Ldap.MembershipVerification;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Server.Pipeline.SecondFactorAuthentication;

public class SecondFactorAuthenticationMiddleware : IRadiusMiddleware
{
    private readonly ISecondFactorChallengeProcessor _challengeProcessor;
    private readonly IMultiFactorApiClient _multiFactorApiClient;
    private readonly IRadiusRequestPostProcessor _requestPostProcessor;
    private readonly ILogger<SecondFactorAuthenticationMiddleware> _logger;

    public SecondFactorAuthenticationMiddleware(
        ISecondFactorChallengeProcessor challengeProcessor,
        IMultiFactorApiClient multiFactorApiClient,
        IRadiusRequestPostProcessor requestPostProcessor,
        ILogger<SecondFactorAuthenticationMiddleware> logger)
    {
        _challengeProcessor = challengeProcessor ?? throw new ArgumentNullException(nameof(challengeProcessor));
        _multiFactorApiClient = multiFactorApiClient ?? throw new ArgumentNullException(nameof(multiFactorApiClient));
        _requestPostProcessor = requestPostProcessor ?? throw new ArgumentNullException(nameof(requestPostProcessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(RadiusContext context, RadiusRequestDelegate next)
    {
        if (context.Authentication.SecondFactor != AuthenticationCode.Awaiting)
        {
            await next(context);
            return;
        }

        if (context.Authentication.SecondFactor == AuthenticationCode.Bypass)
        {
            // second factor not required
            _logger.LogInformation("Bypass second factor for user '{user:l}'", context.UserName);
            context.ResponseCode = context.Authentication.ToPacketCode();

            await next(context);
            return;
        }

        context.ResponseCode = await ProcessSecondAuthenticationFactor(context);
        if (context.ResponseCode == PacketCode.AccessChallenge)
        {
            _challengeProcessor.AddState(new ChallengeRequestIdentifier(context.Configuration, context.State), context);
            return;
        }

        if (context.ResponseCode == PacketCode.AccessAccept)
        {
            context.Authentication.SetSecondFactor(AuthenticationCode.Accept);
        }

        await next(context);
    }

    /// <summary>
    /// Authenticate request at MultiFactor with user-name only
    /// </summary>
    private async Task<PacketCode> ProcessSecondAuthenticationFactor(RadiusContext context)
    {
        if (string.IsNullOrEmpty(context.SecondFactorIdentity))
        {
            _logger.LogWarning("Can't find User-Name in message id={id} from {host:l}:{port}", context.RequestPacket.Header.Identifier, context.RemoteEndpoint.Address, context.RemoteEndpoint.Port);
            return PacketCode.AccessReject;
        }

        if (context.RequestPacket.IsVendorAclRequest)
        {
            // security check
            if (context.Configuration.FirstFactorAuthenticationSource == AuthenticationSource.Radius)
            {
                _logger.LogInformation("Bypass second factor for user {user:l}", context.UserName);
                return PacketCode.AccessAccept;
            }
        }

        return await _multiFactorApiClient.CreateSecondFactorRequest(context);
    }
}