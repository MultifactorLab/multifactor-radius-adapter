using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Core.AccessChallenge;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.MultifactorApi;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;
using Multifactor.Radius.Adapter.v2.Services.Ldap;
using Multifactor.Radius.Adapter.v2.Services.MultifactorApi;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Steps;

public class SecondFactorStep : IRadiusPipelineStep
{
    private readonly IMultifactorApiService _multifactorApiService;
    private readonly IChallengeProcessorProvider _challengeProcessorProvider;
    private readonly ILdapGroupService _ldapGroupService;
    private readonly ILogger<SecondFactorStep> _logger;
    public SecondFactorStep(IMultifactorApiService multifactorApiService, IChallengeProcessorProvider challengeProcessorProvider, ILdapGroupService ldapGroupService, ILogger<SecondFactorStep> logger)
    {
        _multifactorApiService = multifactorApiService;
        _challengeProcessorProvider = challengeProcessorProvider;
        _ldapGroupService = ldapGroupService;
        _logger = logger;
    }

    public async Task ExecuteAsync(IRadiusPipelineExecutionContext context)
    {
        if (!ShouldCallSecondFactor(context.FirstFactorLdapServerConfiguration))
            return;
        
        if (context.RequestPacket.IsVendorAclRequest)
        {
            // security check
            if (context.Settings.FirstFactorAuthenticationSource == AuthenticationSource.Radius)
            {
                _logger.LogInformation("Bypass second factor for user '{user:l}' from {host:l}:{port}",
                    context.RequestPacket.UserName,
                    context.RemoteEndpoint.Address,
                    context.RemoteEndpoint.Port);

                context.AuthenticationState.SecondFactorStatus = AuthenticationStatus.Bypass;
                await Task.CompletedTask;
                return;
            }
        }

        var apiResponse = await _multifactorApiService.CreateSecondFactorRequestAsync(context);
        ProcessApiResponse(context, apiResponse);
    }

    //TODO add 2fa group check
    private bool ShouldCallSecondFactor(ILdapServerConfiguration configuration)
    {
        if (configuration.SecondFaGroups.Any())
            return true;

        if (configuration.SecondFaBypassGroups.Any())
            return false;
        
        return true;
    }

    private void ProcessApiResponse(IRadiusPipelineExecutionContext context, MultifactorResponse apiResponse)
    {
        context.ResponseInformation.State = apiResponse.State;
        context.ResponseInformation.ReplyMessage = apiResponse.ReplyMessage;
        context.AuthenticationState.SecondFactorStatus = apiResponse.Code;

        if (apiResponse.Code == AuthenticationStatus.Awaiting)
        {
            var challengeProcessor =
                _challengeProcessorProvider.GetChallengeProcessorByType(ChallengeType.SecondFactor);
            if (challengeProcessor is null)
                throw new InvalidOperationException("Challenge processor could not be found");
            challengeProcessor.AddChallengeContext(context);
        }
    }
}