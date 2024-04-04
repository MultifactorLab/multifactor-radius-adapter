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
    private readonly IMultifactorApiAdapter _apiAdapter;
    private readonly ILogger<SecondFactorAuthenticationMiddleware> _logger;

    public SecondFactorAuthenticationMiddleware(
        ISecondFactorChallengeProcessor challengeProcessor,
        IMultifactorApiAdapter apiAdapter,
        IRadiusRequestPostProcessor requestPostProcessor,
        ILogger<SecondFactorAuthenticationMiddleware> logger)
    {
        _challengeProcessor = challengeProcessor;
        _apiAdapter = apiAdapter;
        _logger = logger;
    }

    public async Task InvokeAsync(RadiusContext context, RadiusRequestDelegate next)
    {
        var isBypassed = context.Authentication.SecondFactor == AuthenticationCode.Bypass;
        if (isBypassed)
        {
            _logger.LogInformation("Bypass second factor for user '{user:l}' from {host:l}:{port}", context.UserName, context.RemoteEndpoint.Address, context.RemoteEndpoint.Port);
            context.ResponseCode = context.Authentication.ToPacketCode();

            context.ResponseCode = context.Authentication.ToPacketCode();
            await next(context);
            return;
        }

        if (context.Authentication.SecondFactor != AuthenticationCode.Awaiting)
        {
            await next(context);
            return;
        }

        if (string.IsNullOrEmpty(context.SecondFactorIdentity))
        {
            _logger.LogWarning("Unable to process 2FA authentication for message id={id} from {host:l}:{port}: Can't find User-Name",
                context.Header.Identifier, 
                context.RemoteEndpoint.Address, 
                context.RemoteEndpoint.Port); 
            
            context.SetSecondFactorAuth(AuthenticationCode.Reject);
            context.ResponseCode = context.Authentication.ToPacketCode();
            return;
        }

        if (context.RequestPacket.IsVendorAclRequest)
        {
            // security check
            if (context.FirstFactorAuthenticationSource == AuthenticationSource.Radius)
            {
                _logger.LogInformation("Bypass second factor for user '{user:l}' from {host:l}:{port}",
                    context.UserName,
                    context.RemoteEndpoint.Address,
                    context.RemoteEndpoint.Port);

                context.SetSecondFactorAuth(AuthenticationCode.Bypass);
                context.ResponseCode = context.Authentication.ToPacketCode();

                await next(context);
                return;
            }
        }

        var response = await _apiAdapter.CreateSecondFactorRequestAsync(context);
        if (!string.IsNullOrWhiteSpace(response.State))
        {
            context.SetMessageState(response.State);
        }

        context.ReplyMessage = response.ReplyMessage;
        context.SetSecondFactorAuth(response.Code);
        context.ResponseCode = context.Authentication.ToPacketCode();

        if (response.Code == AuthenticationCode.Awaiting)
        {
            _challengeProcessor.AddChallengeContext(context);
            return;
        }

        await next(context);
    }
}