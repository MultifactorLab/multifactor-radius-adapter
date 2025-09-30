using System.Net;
using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap.Attributes;
using Multifactor.Radius.Adapter.v2.Core;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.Auth.PreAuthMode;
using Multifactor.Radius.Adapter.v2.Core.MultifactorApi;
using Multifactor.Radius.Adapter.v2.Core.MultifactorApi.PrivacyMode;
using Multifactor.Radius.Adapter.v2.Exceptions;
using Multifactor.Radius.Adapter.v2.Services.AuthenticatedClientCache;

namespace Multifactor.Radius.Adapter.v2.Services.MultifactorApi;

//TODO separate creation and sending api request
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

    public async Task<MultifactorResponse> CreateSecondFactorRequestAsync(CreateSecondFactorRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        var secondFactorIdentity = GetSecondFactorIdentity(request);
        if (string.IsNullOrWhiteSpace(secondFactorIdentity))
        {
            _logger.LogWarning("Empty user name for second factor request. Request rejected.");
            return new MultifactorResponse(AuthenticationStatus.Reject);
        }

        var personalData = GetPersonalData(request);

        //try to get authenticated client to bypass second factor if configured
        if (_authenticatedClientCache.TryHitCache(personalData.CallingStationId, personalData.Identity, request.ConfigName, request.AuthenticationCacheLifetime))
        {
            _logger.LogInformation(
                "Bypass second factor for user '{user:l}' with calling-station-id {csi:l} from {host:l}:{port}",
                personalData.Identity,
                personalData.CallingStationId,
                request.RemoteEndpoint.Address,
                request.RemoteEndpoint.Port);
            return new MultifactorResponse(AuthenticationStatus.Bypass);
        }

        ApplyPrivacyMode(personalData, request.PrivacyMode);
        var payload = GetRequestPayload(personalData, request);
        
        MultifactorResponse cloudResponse = new MultifactorResponse(AuthenticationStatus.Reject);
        foreach (var apiUrl in request.ApiUrls)
        {
            // TODO move to method
            try
            {
                var response = await CreateAccessRequestAsync(apiUrl, payload, request.ApiCredential);
                var responseCode = ConvertToAuthCode(response);

                if (responseCode == AuthenticationStatus.Reject)
                {
                    var reason = response?.ReplyMessage;
                    var phone = response?.Phone;
                    _logger.LogWarning(
                        "Second factor verification for user '{user:l}' from {host:l}:{port} failed with reason='{reason:l}'. User phone {phone:l}",
                        personalData.Identity,
                        request.RemoteEndpoint.Address,
                        request.RemoteEndpoint.Port,
                        reason,
                        phone);
                }
                
                var mfResponse = new MultifactorResponse(responseCode, state: response?.Id, replyMessage: response?.ReplyMessage);
                
                if (!ShouldCacheResponse(request.ApiResponseCacheEnabled, responseCode, response))
                {
                    _logger.LogDebug("Skip 2FA response caching for user '{user}'.", request.RequestPacket.UserName);
                    return mfResponse;
                }
                
                LogGrantedInfo(personalData.Identity, response, request.RequestPacket.CallingStationIdAttribute);
                _authenticatedClientCache.SetCache(personalData.CallingStationId, personalData.Identity, request.ConfigName, request.AuthenticationCacheLifetime);

                return mfResponse;
            }
            catch (MultifactorApiUnreachableException apiEx)
            {
                cloudResponse = ProcessMfException(apiEx, personalData.Identity,
                    request.BypassSecondFactorWhenApiUnreachable, request.RemoteEndpoint);
            }
            catch (Exception ex)
            {
                cloudResponse = ProcessException(ex, personalData.Identity, request.RemoteEndpoint);
            }
        }

        if (cloudResponse.Code == AuthenticationStatus.Bypass)
            _logger.LogWarning("Bypass second factor for user '{user:l}' from {host:l}:{port}", personalData.Identity, request.RemoteEndpoint.Address, request.RemoteEndpoint.Port);

        return cloudResponse;
    }

    public async Task<MultifactorResponse> SendChallengeAsync(SendChallengeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        ArgumentException.ThrowIfNullOrWhiteSpace(request.RequestId, nameof(request.RequestId));
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Answer, nameof(request.Answer));

        var identity = GetSecondFactorIdentity(request.IdentityAttribute, request.RequestPacket.UserName, request.UserProfile?.Attributes ?? []);
        
        if (string.IsNullOrWhiteSpace(identity))
            throw new InvalidOperationException("The identity is empty.");

        var payload = new ChallengeRequest()
        {
            Identity = identity,
            Challenge = request.Answer,
            RequestId = request.RequestId
        };

        var callingStationIdAttr = request.RequestPacket.CallingStationIdAttribute;
        var callingStationId = GetCallingStationId(callingStationIdAttr, request.RemoteEndpoint);
        MultifactorResponse cloudResponse = new MultifactorResponse(AuthenticationStatus.Reject);
        
        foreach (var apiUrl in request.ApiUrls)
        {
            // TODO move to method
            try
            {
                var response = await _api.SendChallengeAsync(apiUrl, payload, request.ApiCredential);
                var responseCode = ConvertToAuthCode(response);
                
                var mfResponse = new MultifactorResponse(responseCode, state: response?.Id, replyMessage: response?.ReplyMessage);
                
                if (!ShouldCacheResponse(request.ApiResponseCacheEnabled, responseCode, response))
                {
                    _logger.LogDebug("Skip challenge response caching for user '{user}'.", request.RequestPacket.UserName);
                    return mfResponse;
                }
                
                LogGrantedInfo(identity, response, callingStationId);
                _authenticatedClientCache.SetCache(callingStationId, identity, request.ConfigName, request.AuthenticationCacheLifetime);

                return mfResponse;
            }
            catch (MultifactorApiUnreachableException apiEx)
            {
                cloudResponse = ProcessMfException(apiEx, identity, request.BypassSecondFactorWhenApiUnreachable, request.RemoteEndpoint);
            }
            catch (Exception ex)
            {
                cloudResponse = ProcessException(ex, identity, request.RemoteEndpoint);
            }
        }

        if (cloudResponse.Code == AuthenticationStatus.Bypass)
            _logger.LogWarning("Bypass second factor for user '{user:l}' from {host:l}:{port}", identity,
                request.RemoteEndpoint.Address, request.RemoteEndpoint.Port);

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

    private static string? GetPassCodeOrNull(CreateSecondFactorRequest context)
    {
        //check static challenge
        var challenge = context.RequestPacket.TryGetChallenge();
        if (challenge != null)
        {
            return challenge;
        }

        //check password challenge (otp or passcode)
        var passphrase = context.Passphrase;
        switch (context.PreAuthnMode.Mode)
        {
            case PreAuthMode.Otp:
                return passphrase.Otp;

            case PreAuthMode.Push:
                return "m";

            case PreAuthMode.Telegram:
                return "t";
        }

        if (passphrase.IsEmpty)
            return null;

        if (context.FirstFactorAuthenticationSource != AuthenticationSource.None)
            return null;

        return passphrase.Otp ?? passphrase.ProviderCode;
    }

    private async Task<AccessRequestResponse?> CreateAccessRequestAsync(string address, AccessRequest payload, ApiCredential apiCredential)
    {
        var response = await _api.CreateAccessRequest(address, payload, apiCredential);
        return response;
    }

    private string? GetSecondFactorIdentity(CreateSecondFactorRequest context)
    {
        if (string.IsNullOrWhiteSpace(context.IdentityAttribute))
            return context.RequestPacket.UserName;

        return context.UserProfile?.Attributes
            .FirstOrDefault(x => x.Name == context.IdentityAttribute)?.Values
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

    private PersonalData GetPersonalData(CreateSecondFactorRequest request)
    {
        var secondFactorIdentity = GetSecondFactorIdentity(request);
        var callingStationId = request.RequestPacket.CallingStationIdAttribute;

        var callingStationIdForApiRequest = GetCallingStationId(callingStationId, request.RemoteEndpoint);

        var phone = request.UserProfile?.Attributes
            .Where(x => request.PhoneAttributesNames.Contains(x.Name.Value))
            .Select(x => x.GetNotEmptyValues().FirstOrDefault())
            .FirstOrDefault();

        var personalData = new PersonalData
        {
            Identity = secondFactorIdentity!,
            DisplayName = request.UserProfile?.DisplayName,
            Email = request.UserProfile?.Email,
            Phone = string.IsNullOrWhiteSpace(phone) ? request.UserProfile?.Phone : phone,
            CalledStationId = request.RequestPacket.CalledStationIdAttribute,
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

    private AccessRequest GetRequestPayload(PersonalData personalData, CreateSecondFactorRequest context)
    {
        return new AccessRequest
        {
            Identity = UserNameTransformation.Transform(personalData.Identity, context.UserNameTransformRules.BeforeSecondFactor),
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
                SignUpGroups = context.SignUpGroups ?? string.Empty
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
                    pd.DisplayName = null;

                if (!modeDescriptor.HasField("Email"))
                    pd.Email = null;

                if (!modeDescriptor.HasField("Phone"))
                    pd.Phone = null;

                if (!modeDescriptor.HasField("RemoteHost"))
                    pd.CallingStationId = "";

                pd.CalledStationId = null;

                break;
        }
    }

    private MultifactorResponse ProcessMfException(MultifactorApiUnreachableException apiEx, string identity, bool bypassSecondFactorWhenApiUnreachable, IPEndPoint remoteEndpoint)
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
            return new MultifactorResponse(radCode);
        }

        var code = ConvertToAuthCode(AccessRequestResponse.Bypass);
        return new MultifactorResponse(code);
    }

    private MultifactorResponse ProcessException(Exception ex, string identity, IPEndPoint remoteEndpoint)
    {
        _logger.LogError(ex, "Error occured while requesting API for user '{user:l}' from {host:l}:{port}, {msg:l}",
            identity,
            remoteEndpoint.Address,
            remoteEndpoint.Port,
            ex.Message);

        var code = ConvertToAuthCode(null);
        return new MultifactorResponse(code);
    }

    private bool ShouldCacheResponse(bool apiResponseCacheEnabled, AuthenticationStatus responseCode,  AccessRequestResponse? response) => apiResponseCacheEnabled && responseCode == AuthenticationStatus.Accept && !(response?.Bypassed ?? false);
}