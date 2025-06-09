using System.Net;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Core;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.Auth.PreAuthMode;
using Multifactor.Radius.Adapter.v2.Core.MultifactorApi;
using Multifactor.Radius.Adapter.v2.Core.MultifactorApi.PrivacyMode;
using Multifactor.Radius.Adapter.v2.Exceptions;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;
using Multifactor.Radius.Adapter.v2.Services.AuthenticatedClientCache;

namespace Multifactor.Radius.Adapter.v2.Services.MultifactorApi;

public class MultifactorApiService : IMultifactorApiService
{
    private readonly IMultifactorApi _api;
    private readonly IAuthenticatedClientCache _authenticatedClientCache;
    private readonly ILogger<MultifactorApiService> _logger;

    public MultifactorApiService(IMultifactorApi api, IAuthenticatedClientCache authenticatedClientCache, ILogger<MultifactorApiService> logger)
    {
        ArgumentNullException.ThrowIfNull(api, nameof(api));
        ArgumentNullException.ThrowIfNull(authenticatedClientCache, nameof(authenticatedClientCache));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));

        _api = api;
        _authenticatedClientCache = authenticatedClientCache;
        _logger = logger;
    }

    public async Task<MultifactorResponse> CreateSecondFactorRequestAsync(IRadiusPipelineExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        var secondFactorIdentity = GetSecondFactorIdentity(context);
        if (string.IsNullOrWhiteSpace(secondFactorIdentity))
        {
            _logger.LogWarning("Empty user name for second factor request. Request rejected.");
            return new MultifactorResponse(AuthenticationStatus.Reject);
        }

        var personalData = GetPersonalData(context);
        var callingStationId = context.RequestPacket.CallingStationIdAttribute;

        //try to get authenticated client to bypass second factor if configured
        if (_authenticatedClientCache.TryHitCache(callingStationId, personalData.Identity, context.Settings))
        {
            _logger.LogInformation(
                "Bypass second factor for user '{user:l}' with calling-station-id {csi:l} from {host:l}:{port}",
                personalData.Identity,
                callingStationId,
                context.RemoteEndpoint.Address,
                context.RemoteEndpoint.Port);
            return new MultifactorResponse(AuthenticationStatus.Bypass);
        }

        ApplyPrivacyMode(personalData, context.Settings.PrivacyMode);

        var payload = GetRequestPayload(personalData, context);

        try
        {
            var response = await CreateAccessRequestAsync(personalData, payload, context);
            var responseCode = ConvertToAuthCode(response);
            if (responseCode == AuthenticationStatus.Accept && !(response?.Bypassed ?? false))
            {
                LogGrantedInfo(personalData.Identity, response, context);
                _authenticatedClientCache.SetCache(callingStationId, personalData.Identity, context.Settings);
            }

            return new MultifactorResponse(responseCode, response?.Id, response?.ReplyMessage);
        }
        catch (MultifactorApiUnreachableException apiEx)
        {
            return ProcessMfException(apiEx, personalData.Identity, context);
        }
        catch (Exception ex)
        {
            return ProcessException(ex, personalData.Identity, context);
        }
    }

    public async Task<MultifactorResponse> SendChallengeAsync(IRadiusPipelineExecutionContext context, string answer, string requestId)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentException.ThrowIfNullOrWhiteSpace(requestId, nameof(requestId));
        ArgumentException.ThrowIfNullOrWhiteSpace(answer, nameof(answer));

        var identity = GetSecondFactorIdentity(context);
        if (string.IsNullOrWhiteSpace(identity))
            throw new InvalidOperationException("The identity is empty.");

        var payload = new ChallengeRequest()
        {
            Identity = identity,
            Challenge = answer,
            RequestId = requestId
        };

        try
        {
            var response = await _api.SendChallengeAsync(payload, context.Settings.ApiCredential);
            var responseCode = ConvertToAuthCode(response);
            if (responseCode == AuthenticationStatus.Accept && !response.Bypassed)
            {
                LogGrantedInfo(identity, response, context);
                _authenticatedClientCache.SetCache(context.RequestPacket.CallingStationIdAttribute, identity, context.Settings);
            }

            return new MultifactorResponse(responseCode, response?.ReplyMessage);
        }
        catch (MultifactorApiUnreachableException apiEx)
        {
            return ProcessMfException(apiEx, identity, context);
        }
        catch (Exception ex)
        {
            return ProcessException(ex, identity, context);
        }
    }

    private AuthenticationStatus ConvertToAuthCode(AccessRequestResponse? multifactorAccessRequest)
    {
        if (multifactorAccessRequest == null)
        {
            return AuthenticationStatus.Reject;
        }

        switch (multifactorAccessRequest.Status)
        {
            case RequestStatus.Granted when multifactorAccessRequest.Bypassed:
                return AuthenticationStatus.Bypass;

            case RequestStatus.Granted:
                return AuthenticationStatus.Accept;

            case RequestStatus.Denied:
                return AuthenticationStatus.Reject;

            case RequestStatus.AwaitingAuthentication:
                return AuthenticationStatus.Awaiting;

            default:
                _logger.LogWarning("Got unexpected status from API: {status:l}", multifactorAccessRequest.Status);
                return AuthenticationStatus.Reject;
        }
    }

    private void LogGrantedInfo(string identity, AccessRequestResponse? response, IRadiusPipelineExecutionContext context)
    {
        string countryValue = null;
        string regionValue = null;
        string cityValue = null;
        string? callingStationId = context?.RequestPacket?.CallingStationIdAttribute;

        if (response != null && IPAddress.TryParse(callingStationId, out var ip))
        {
            countryValue = response.CountryCode;
            regionValue = response.Region;
            cityValue = response.City;
            callingStationId = ip.ToString();
        }

        _logger.LogInformation(
            "Second factor for user '{user:l}' verified successfully. Authenticator: '{authenticator:l}', account: '{account:l}', country: '{country:l}', region: '{region:l}', city: '{city:l}', calling-station-id: {clientIp}, authenticatorId: {authenticatorId}",
            identity,
            response?.Authenticator,
            response?.Account,
            countryValue,
            regionValue,
            cityValue,
            callingStationId,
            response!.AuthenticatorId);
    }

    private static string? GetPassCodeOrNull(IRadiusPipelineExecutionContext context)
    {
        //check static challenge
        var challenge = context.RequestPacket.TryGetChallenge();
        if (challenge != null)
        {
            return challenge;
        }

        //check password challenge (otp or passcode)
        var passphrase = context.Passphrase;
        switch (context.Settings.PreAuthnMode.Mode)
        {
            case PreAuthMode.Otp:
                return passphrase.Otp;

            case PreAuthMode.Push:
                return "m";

            case PreAuthMode.Telegram:
                return "t";
        }

        if (passphrase.IsEmpty)
        {
            return null;
        }

        if (context.Settings.FirstFactorAuthenticationSource != AuthenticationSource.None)
        {
            return null;
        }

        return context.Passphrase.Otp ?? passphrase.ProviderCode;
    }

    private async Task<AccessRequestResponse?> CreateAccessRequestAsync(PersonalData personalData, AccessRequest payload, IRadiusPipelineExecutionContext context)
    {
        var response = await _api.CreateAccessRequest(payload, context.Settings.ApiCredential);
        var responseCode = ConvertToAuthCode(response);

        if (responseCode == AuthenticationStatus.Reject)
        {
            var reason = response?.ReplyMessage;
            var phone = response?.Phone;
            _logger.LogWarning(
                "Second factor verification for user '{user:l}' from {host:l}:{port} failed with reason='{reason:l}'. User phone {phone:l}",
                personalData.Identity,
                context.RemoteEndpoint.Address,
                context.RemoteEndpoint.Port,
                reason,
                phone);
        }

        return response;
    }

    private string? GetSecondFactorIdentity(IRadiusPipelineExecutionContext context)
    {
        if (string.IsNullOrWhiteSpace(context.FirstFactorLdapServerConfiguration?.IdentityAttribute))
            return context.RequestPacket.UserName;

        return context.UserLdapProfile?.Attributes
            .FirstOrDefault(x => x.Name == context.FirstFactorLdapServerConfiguration.IdentityAttribute)?.Values
            .FirstOrDefault();
    }

    private PersonalData GetPersonalData(IRadiusPipelineExecutionContext context)
    {
        var secondFactorIdentity = GetSecondFactorIdentity(context);
        var callingStationId = context.RequestPacket.CallingStationIdAttribute;
        // CallingStationId may contain hostname. For IP policy to work correctly in MF cloud we need IP instead of hostname
        var callingStationIdForApiRequest = IPAddress.TryParse(callingStationId ?? string.Empty, out _)
            ? callingStationId
            : context.RemoteEndpoint.Address.ToString();
        var personalData = new PersonalData
        {
            Identity = secondFactorIdentity!,
            DisplayName = context.UserLdapProfile.DisplayName,
            Email = context.UserLdapProfile.Email,
            Phone = context.UserLdapProfile.Phone,
            CalledStationId = context.RequestPacket.CalledStationIdAttribute,
            CallingStationId = callingStationIdForApiRequest
        };

        return personalData;
    }

    private AccessRequest GetRequestPayload(PersonalData personalData, IRadiusPipelineExecutionContext context)
    {
        return new AccessRequest
        {
            Identity = UserNameTransformation.Transform(personalData.Identity,
                context.Settings.UserNameTransformRules.BeforeSecondFactor),
            Name = personalData.DisplayName,
            Email = personalData.Email,
            Phone = personalData.Phone,
            PassCode = GetPassCodeOrNull(context),
            CallingStationId = personalData.CallingStationId,
            CalledStationId = personalData.CalledStationId,
            Capabilities = new Capabilities
            {
                InlineEnroll = true
            },
            GroupPolicyPreset = new GroupPolicyPreset
            {
                SignUpGroups = context.Settings.SignUpGroups
            }
        };
    }

    private void ApplyPrivacyMode(PersonalData pd, PrivacyModeDescriptor modeDescriptor)
    {
        //remove user information for privacy
        switch (modeDescriptor.Mode)
        {
            case PrivacyMode.Full:
                pd.DisplayName = null;
                pd.Email = null;
                pd.Phone = null;
                pd.CallingStationId = "";
                pd.CalledStationId = null;
                break;

            case PrivacyMode.Partial:
                if (!modeDescriptor.HasField("Name"))
                {
                    pd.DisplayName = null;
                }

                if (!modeDescriptor.HasField("Email"))
                {
                    pd.Email = null;
                }

                if (!modeDescriptor.HasField("Phone"))
                {
                    pd.Phone = null;
                }

                if (!modeDescriptor.HasField("RemoteHost"))
                {
                    pd.CallingStationId = "";
                }

                pd.CalledStationId = null;

                break;
        }
    }
    
    private MultifactorResponse ProcessMfException(MultifactorApiUnreachableException apiEx, string identity, IRadiusPipelineExecutionContext context)
    {
        _logger.LogError(apiEx,
            "Error occured while requesting API for user '{user:l}' from {host:l}:{port}, {msg:l}",
            identity,
            context.RemoteEndpoint.Address,
            context.RemoteEndpoint.Port,
            apiEx.Message);

        if (!context.Settings.BypassSecondFactorWhenApiUnreachable)
        {
            var radCode = ConvertToAuthCode(null);
            return new MultifactorResponse(radCode);
        }

        _logger.LogWarning("Bypass second factor for user '{user:l}' from {host:l}:{port}",
            identity,
            context.RemoteEndpoint.Address,
            context.RemoteEndpoint.Port);

        var code = ConvertToAuthCode(AccessRequestResponse.Bypass);
        return new MultifactorResponse(code);
    }
    
    private MultifactorResponse ProcessException(Exception ex, string identity, IRadiusPipelineExecutionContext context)
     {
         _logger.LogError(ex, "Error occured while requesting API for user '{user:l}' from {host:l}:{port}, {msg:l}",
             identity,
             context.RemoteEndpoint.Address,
             context.RemoteEndpoint.Port,
             ex.Message);

         var code = ConvertToAuthCode(null);
         return new MultifactorResponse(code);
    }
}