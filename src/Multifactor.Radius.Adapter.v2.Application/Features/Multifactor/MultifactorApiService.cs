using System.Net;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Cache;
using Multifactor.Radius.Adapter.v2.Application.Configuration.Models.Enum;
using Multifactor.Radius.Adapter.v2.Application.Features.Multifactor.Exceptions;
using Multifactor.Radius.Adapter.v2.Application.Features.Multifactor.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Multifactor.Models.Enum;
using Multifactor.Radius.Adapter.v2.Application.Features.Multifactor.Ports;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.AccessChallenge.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models.Enum;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Multifactor;

public sealed class MultifactorApiService
{
    private readonly IMultifactorApi _api;
    private readonly IAuthenticatedClientCache _authenticatedClientCache;
    private readonly ILogger<MultifactorApiService> _logger;

    public MultifactorApiService(
        IMultifactorApi api,
        IAuthenticatedClientCache authenticatedClientCache,
        ILogger<MultifactorApiService> logger)
    {
        ArgumentNullException.ThrowIfNull(api, nameof(api));
        ArgumentNullException.ThrowIfNull(authenticatedClientCache, nameof(authenticatedClientCache));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));
        _api = api;
        _authenticatedClientCache = authenticatedClientCache;
        _logger = logger;
    }

    public async Task<SecondFactorResponse> CreateSecondFactorRequestAsync(RadiusPipelineContext context, bool cacheEnabled)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        _logger.LogInformation($"Creating second-factor request for user {context.RequestPacket.UserName}");
        var personalData = RequestDataExtractor.ExtractPersonalData(context);
        if (string.IsNullOrWhiteSpace(personalData.Identity))
        {
            _logger.LogWarning("Empty user name for second factor context. Request rejected.");
            return new SecondFactorResponse(AuthenticationStatus.Reject);
        }

        if (_authenticatedClientCache.TryHitCache(
            personalData.CallingStationId, 
            personalData.Identity, 
            context.ClientConfiguration.Name, 
            context.ClientConfiguration.AuthenticationCacheLifetime))
        {
            _logger.LogInformation(
                "Bypass second factor for user '{user:l}' with calling-station-id {csi:l} from {host:l}:{port}",
                personalData.Identity,
                personalData.CallingStationId,
                context.RequestPacket.RemoteEndpoint.Address,
                context.RequestPacket.RemoteEndpoint.Port);
            return new SecondFactorResponse(AuthenticationStatus.Bypass);
        }

        ApplyPrivacyMode(ref personalData, context.ClientConfiguration.Privacy.PrivacyMode, context.ClientConfiguration.Privacy.PrivacyFields);

        try
        {
            var request = CreateAccessRequestQuery(personalData, context);
            var authData = new MultifactorAuthData(
                context.ClientConfiguration.MultifactorNasIdentifier, 
                context.ClientConfiguration.MultifactorSharedSecret);
            
            var response = await _api.CreateAccessRequest(request, authData);
            var responseCode = ConvertToAuthCode(response);

            if (responseCode == AuthenticationStatus.Reject)
            {
                var reason = response?.ReplyMessage;
                var phone2 = response?.Phone;
                _logger.LogWarning(
                    "Second factor verification for user '{user:l}' from {host:l}:{port} failed with reason='{reason:l}'. User phone {phone:l}",
                    personalData.Identity,
                    context.RequestPacket.RemoteEndpoint.Address,
                    context.RequestPacket.RemoteEndpoint.Port,
                    reason,
                    phone2);
            }
            
            var mfResponse = new SecondFactorResponse(responseCode, state: response?.Id, replyMessage: response?.ReplyMessage);
            
            if (!ShouldCacheResponse(cacheEnabled, responseCode, response))
            {
                _logger.LogDebug("Skip 2FA response caching for user '{user}'.", context.RequestPacket.UserName);
                return mfResponse;
            }
            
            LogGrantedInfo(personalData.Identity, response, context.RequestPacket.CallingStationIdAttribute);
            _authenticatedClientCache.SetCache(
                personalData.CallingStationId, 
                personalData.Identity, 
                context.ClientConfiguration.Name, 
                context.ClientConfiguration.AuthenticationCacheLifetime);

            return mfResponse;
        }
        catch (MultifactorApiUnreachableException apiEx)
        {
            return ProcessMfException(apiEx, personalData.Identity,
                context.ClientConfiguration.BypassSecondFactorWhenApiUnreachable, 
                context.LdapConfiguration?.BypassSecondFactorWhenApiUnreachableGroups, 
                context.UserGroups, context.RequestPacket.RemoteEndpoint);
        }
        catch (Exception ex)
        {
            return ProcessException(ex, personalData.Identity, context.RequestPacket.RemoteEndpoint);
        }
    }

    public async Task<SecondFactorResponse> SendChallengeAsync(RadiusPipelineContext context, bool cacheEnabled, string requestId, string answer)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentException.ThrowIfNullOrWhiteSpace(requestId, nameof(requestId));
        ArgumentException.ThrowIfNullOrWhiteSpace(answer, nameof(answer));

        var identity = RequestDataExtractor.GetSecondFactorIdentity(context);
        if (string.IsNullOrWhiteSpace(identity))
            throw new InvalidOperationException("The identity is empty.");

        var dto = new ChallengeRequestQuery
        {
            Identity = identity,
            Challenge = answer,
            RequestId = requestId
        };

        var callingStationIdAttr = context.RequestPacket.CallingStationIdAttribute;
        var callingStationId = RequestDataExtractor.GetCallingStationId(callingStationIdAttr, context.RequestPacket.RemoteEndpoint);
        
        try
        {
            var authData = new MultifactorAuthData(
                context.ClientConfiguration.MultifactorNasIdentifier, 
                context.ClientConfiguration.MultifactorSharedSecret);
            
            var response = await _api.SendChallengeAsync(dto, authData);
            var responseCode = ConvertToAuthCode(response);
            
            var mfResponse = new SecondFactorResponse(responseCode, state: response?.Id, replyMessage: response?.ReplyMessage);
            
            if (!ShouldCacheResponse(cacheEnabled, responseCode, response))
            {
                _logger.LogDebug("Skip challenge response caching for user '{user}'.", context.RequestPacket.UserName);
                return mfResponse;
            }
            
            LogGrantedInfo(identity, response, callingStationId);
            _authenticatedClientCache.SetCache(
                callingStationId, 
                identity, 
                context.ClientConfiguration.Name, 
                context.ClientConfiguration.AuthenticationCacheLifetime);

            return mfResponse;
        }
        catch (MultifactorApiUnreachableException apiEx)
        {
            return ProcessMfException(apiEx, identity,
                context.ClientConfiguration.BypassSecondFactorWhenApiUnreachable, 
                context.LdapConfiguration?.BypassSecondFactorWhenApiUnreachableGroups, 
                context.UserGroups, context.RequestPacket.RemoteEndpoint);
        }
        catch (Exception ex)
        {
            return ProcessException(ex, identity, context.RequestPacket.RemoteEndpoint);
        }
    }

    private AuthenticationStatus ConvertToAuthCode(AccessRequestResponse? multifactorAccessRequest)
    {
        if (multifactorAccessRequest == null)
            return AuthenticationStatus.Reject;

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

    private void LogGrantedInfo(string identity, AccessRequestResponse? response, string? callingStationIdAttribute)
    {
        string? countryValue = null;
        string? regionValue = null;
        string? cityValue = null;
        var callingStationId = callingStationIdAttribute;

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
            response?.AuthenticatorId);
    }

    private AccessRequestQuery CreateAccessRequestQuery(PersonalData personalData, RadiusPipelineContext context)
    {
        var phone = RequestDataExtractor.GetUserPhone(context);
        
        return new AccessRequestQuery
        {
            Identity = personalData.Identity,
            Name = personalData.DisplayName,
            Email = personalData.Email,
            Phone = string.IsNullOrWhiteSpace(phone) ? personalData.Phone : phone,
            CalledStationId = personalData.CalledStationId,
            CallingStationId = personalData.CallingStationId,
            SignUpGroups = string.Join(';', context.ClientConfiguration.SignUpGroups),
            PassCode = GetPassCodeOrNull(context)
        };
    }
    
    private static string? GetPassCodeOrNull(RadiusPipelineContext context)
    {
        //check static challenge
        var challenge = context.RequestPacket.TryGetChallenge();
        if (challenge != null)
        {
            return challenge;
        }

        //check password challenge (otp or passcode)
        var passphrase = context.Passphrase;
        switch (context.ClientConfiguration.PreAuthenticationMethod)
        {
            case PreAuthMode.Otp:
                return passphrase.Otp;
        }

        if (passphrase.IsEmpty)
            return null;

        if (context.ClientConfiguration.FirstFactorAuthenticationSource != AuthenticationSource.None)
            return null;

        return passphrase.Otp ?? passphrase.ProviderCode;
    }
    
    private static void ApplyPrivacyMode(ref PersonalData pd, PrivacyMode mode, string[] privacyFields)
    {
        switch (mode)
        {
            case PrivacyMode.Full:
                pd.DisplayName = null;
                pd.Email = null;
                pd.Phone = null;
                pd.CallingStationId = "";
                pd.CalledStationId = null;
                break;

            case PrivacyMode.Partial:
                if (!privacyFields.Contains("Name"))
                    pd.DisplayName = null;

                if (!privacyFields.Contains("Email"))
                    pd.Email = null;

                if (!privacyFields.Contains("Phone"))
                    pd.Phone = null;

                if (!privacyFields.Contains("RemoteHost"))
                    pd.CallingStationId = "";

                pd.CalledStationId = null;
                break;
        }
    }

    private SecondFactorResponse ProcessMfException(
        MultifactorApiUnreachableException apiEx, 
        string identity, 
        bool bypassSecondFactorWhenApiUnreachable, 
        IReadOnlyList<string> bypassSecondFactorWhenApiUnreachableGroups,
        HashSet<string> userGroups,
        IPEndPoint remoteEndpoint)
    {
        _logger.LogError(apiEx,
            "Error occured while requesting API for user '{user:l}' from {host:l}:{port}, {msg:l}",
            identity,
            remoteEndpoint.Address,
            remoteEndpoint.Port,
            apiEx.Message);
        
        if (bypassSecondFactorWhenApiUnreachable && 
                (!bypassSecondFactorWhenApiUnreachableGroups.Any() 
                || bypassSecondFactorWhenApiUnreachableGroups.Intersect(userGroups).Any())
            )
        {
            var code = ConvertToAuthCode(AccessRequestResponse.Bypass);
            return new SecondFactorResponse(code);
        }
        
        var radCode = ConvertToAuthCode(null);
        return new SecondFactorResponse(radCode);
    }

    private SecondFactorResponse ProcessException(Exception ex, string identity, IPEndPoint remoteEndpoint)
    {
        _logger.LogError(ex, 
            "Error occured while requesting API for user '{user:l}' from {host:l}:{port}, {msg:l}",
            identity,
            remoteEndpoint.Address,
            remoteEndpoint.Port,
            ex.Message);

        var code = ConvertToAuthCode(null);
        return new SecondFactorResponse(code);
    }

    private static bool ShouldCacheResponse(bool apiResponseCacheEnabled, AuthenticationStatus responseCode, AccessRequestResponse? response) 
        => apiResponseCacheEnabled && responseCode == AuthenticationStatus.Accept && !(response?.Bypassed ?? false);
}