using System.Net;
using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap.Attributes;
using Multifactor.Radius.Adapter.v2.Application.Cache;
using Multifactor.Radius.Adapter.v2.Application.Features.AccessChallenge.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Multifactor.Exceptions;
using Multifactor.Radius.Adapter.v2.Application.Features.Multifactor.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Multifactor.Ports;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;
using Multifactor.Radius.Adapter.v2.Application.Models.Enum;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Multifactor;

//TODO separate creation and sending api context
public class MultifactorApiService
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

    public async Task<SecondFactorResponse> CreateSecondFactorRequestAsync(RadiusPipelineContext context, bool cacheEnabled)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        var secondFactorIdentity = GetSecondFactorIdentity(context);
        if (string.IsNullOrWhiteSpace(secondFactorIdentity))
        {
            _logger.LogWarning("Empty user name for second factor context. Request rejected.");
            return new SecondFactorResponse(AuthenticationStatus.Reject);
        }

        var personalData = GetPersonalData(context);

        //try to get authenticated client to bypass second factor if configured
        if (_authenticatedClientCache.TryHitCache(personalData.CallingStationId, personalData.Identity, context.ClientConfiguration.Name, context.ClientConfiguration.AuthenticationCacheLifetime))
        {
            _logger.LogInformation(
                "Bypass second factor for user '{user:l}' with calling-station-id {csi:l} from {host:l}:{port}",
                personalData.Identity,
                personalData.CallingStationId,
                context.RequestPacket.RemoteEndpoint.Address,
                context.RequestPacket.RemoteEndpoint.Port);
            return new SecondFactorResponse(AuthenticationStatus.Bypass);
        }

        ApplyPrivacyMode(personalData, context.ClientConfiguration.PrivacyMode, context.ClientConfiguration.PrivacyFields);
        
        SecondFactorResponse cloudResponse;

        // TODO move to method
        try
        {
            var phone = context.LdapProfile?.Attributes
                .Where(x => context.LdapConfiguration.PhoneAttributes.Contains(x.Name.Value))
                .Select(x => x.GetNotEmptyValues().FirstOrDefault())
                .FirstOrDefault();
            var dto = new AccessRequestQuery
            {
                Identity = personalData.Identity,
                Name = context.LdapProfile.DisplayName,
                Email = context.LdapProfile.Email,
                Phone = string.IsNullOrWhiteSpace(phone) ? context.LdapProfile?.Phone : phone,
                CalledStationId = context.RequestPacket.CalledStationIdAttribute,
                CallingStationId = personalData.CallingStationId
            };
            var response = await _api.CreateAccessRequest(dto);
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
            _authenticatedClientCache.SetCache(personalData.CallingStationId, personalData.Identity, context.ClientConfiguration.Name, context.ClientConfiguration.AuthenticationCacheLifetime);

            return mfResponse;
        }
        catch (MultifactorApiUnreachableException apiEx)
        {
            cloudResponse = ProcessMfException(apiEx, personalData.Identity,
                context.ClientConfiguration.BypassSecondFactorWhenApiUnreachable, context.RequestPacket.RemoteEndpoint);
        }
        catch (Exception ex)
        {
            cloudResponse = ProcessException(ex, personalData.Identity, context.RequestPacket.RemoteEndpoint);
        }
        

        if (cloudResponse.Code == AuthenticationStatus.Bypass)
            _logger.LogWarning("Bypass second factor for user '{user:l}' from {host:l}:{port}", personalData.Identity, context.RequestPacket.RemoteEndpoint.Address, context.RequestPacket.RemoteEndpoint.Port);

        return cloudResponse;
    }

    public async Task<SecondFactorResponse> SendChallengeAsync(RadiusPipelineContext context, bool cacheEnabled, string requestId, string answer)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentException.ThrowIfNullOrWhiteSpace(requestId, nameof(requestId));
        ArgumentException.ThrowIfNullOrWhiteSpace(answer, nameof(answer));

        var identity = GetSecondFactorIdentity(context.LdapConfiguration.IdentityAttribute, context.RequestPacket.UserName, context.LdapProfile?.Attributes ?? []);
        
        if (string.IsNullOrWhiteSpace(identity))
            throw new InvalidOperationException("The identity is empty.");

        var dto = new ChallengeRequestQuery
        {
            Identity = identity,
            Challenge = answer,
            RequestId = requestId
        };

        var callingStationIdAttr = context.RequestPacket.CallingStationIdAttribute;
        var callingStationId = GetCallingStationId(callingStationIdAttr, context.RequestPacket.RemoteEndpoint);
        SecondFactorResponse cloudResponse;
        
        try
        {
            var response = await _api.SendChallengeAsync(dto);
            var responseCode = ConvertToAuthCode(response);
            
            var mfResponse = new SecondFactorResponse(responseCode, state: response?.Id, replyMessage: response?.ReplyMessage);
            
            if (!ShouldCacheResponse(cacheEnabled, responseCode, response))
            {
                _logger.LogDebug("Skip challenge response caching for user '{user}'.", context.RequestPacket.UserName);
                return mfResponse;
            }
            
            LogGrantedInfo(identity, response, callingStationId);
            _authenticatedClientCache.SetCache(callingStationId, identity, context.ClientConfiguration.Name, context.ClientConfiguration.AuthenticationCacheLifetime);

            return mfResponse;
        }
        catch (MultifactorApiUnreachableException apiEx)
        {
            cloudResponse = ProcessMfException(apiEx, identity, context.ClientConfiguration.BypassSecondFactorWhenApiUnreachable, context.RequestPacket.RemoteEndpoint);
        }
        catch (Exception ex)
        {
            cloudResponse = ProcessException(ex, identity, context.RequestPacket.RemoteEndpoint);
        }

        if (cloudResponse.Code == AuthenticationStatus.Bypass)
            _logger.LogWarning("Bypass second factor for user '{user:l}' from {host:l}:{port}", identity,
                context.RequestPacket.RemoteEndpoint.Address, context.RequestPacket.RemoteEndpoint.Port);

        return cloudResponse;
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

    private string? GetSecondFactorIdentity(RadiusPipelineContext context)
    {
        if (string.IsNullOrWhiteSpace(context.LdapConfiguration.IdentityAttribute))
            return context.RequestPacket.UserName;

        return context.LdapProfile?.Attributes
            .FirstOrDefault(x => x.Name == context.LdapConfiguration.IdentityAttribute)?.Values
            .FirstOrDefault();
    }

    private string? GetSecondFactorIdentity(string? identityAttribute, string? userName,
        IReadOnlyCollection<LdapAttribute> profileAttributes)
    {
        if (string.IsNullOrWhiteSpace(identityAttribute))
            return userName;

        return profileAttributes
            .FirstOrDefault(x => x.Name == identityAttribute)?.Values
            .FirstOrDefault();
    }

    private PersonalData GetPersonalData(RadiusPipelineContext context)
    {
        var secondFactorIdentity = GetSecondFactorIdentity(context);
        var callingStationId = context.RequestPacket.CallingStationIdAttribute;

        var callingStationIdForApiRequest = GetCallingStationId(callingStationId, context.RequestPacket.RemoteEndpoint);

        var phone = context.LdapProfile?.Attributes
            .Where(x => context.LdapProfile.Phone.Contains(x.Name.Value))
            .Select(x => x.GetNotEmptyValues().FirstOrDefault())
            .FirstOrDefault();

        var personalData = new PersonalData
        {
            Identity = secondFactorIdentity!,
            DisplayName = context.LdapProfile?.DisplayName,
            Email = context.LdapProfile?.Email,
            Phone = string.IsNullOrWhiteSpace(phone) ? context.LdapProfile?.Phone : phone,
            CalledStationId = context.RequestPacket.CalledStationIdAttribute,
            CallingStationId = callingStationIdForApiRequest
        };

        return personalData;
    }

    private string? GetCallingStationId(string? callingStationIdAttributeValue, IPEndPoint remoteEndPoint)
    {
        // CallingStationId may contain hostname. For IP policy to work correctly in MF cloud we need IP instead of hostname
        return IPAddress.TryParse(callingStationIdAttributeValue ?? string.Empty, out _)
            ? callingStationIdAttributeValue
            : remoteEndPoint.Address.ToString();
    }

    private void ApplyPrivacyMode(PersonalData pd, PrivacyMode mode, string[] privacyFields)
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

    private SecondFactorResponse ProcessMfException(MultifactorApiUnreachableException apiEx, string identity, bool bypassSecondFactorWhenApiUnreachable, IPEndPoint remoteEndpoint)
    {
        _logger.LogError(apiEx,
            "Error occured while requesting API for user '{user:l}' from {host:l}:{port}, {msg:l}",
            identity,
            remoteEndpoint.Address,
            remoteEndpoint.Port,
            apiEx.Message);

        if (!bypassSecondFactorWhenApiUnreachable)
        {
            var radCode = ConvertToAuthCode(null);
            return new SecondFactorResponse(radCode);
        }

        var code = ConvertToAuthCode(AccessRequestResponse.Bypass);
        return new SecondFactorResponse(code);
    }

    private SecondFactorResponse ProcessException(Exception ex, string identity, IPEndPoint remoteEndpoint)
    {
        _logger.LogError(ex, "Error occured while requesting API for user '{user:l}' from {host:l}:{port}, {msg:l}",
            identity,
            remoteEndpoint.Address,
            remoteEndpoint.Port,
            ex.Message);

        var code = ConvertToAuthCode(null);
        return new SecondFactorResponse(code);
    }

    private static bool ShouldCacheResponse(bool apiResponseCacheEnabled, AuthenticationStatus responseCode,  AccessRequestResponse? response) => apiResponseCacheEnabled && responseCode == AuthenticationStatus.Accept && !(response?.Bypassed ?? false);
}