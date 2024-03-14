//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md


using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Configuration.Features.PrivacyModeFeature;
using MultiFactor.Radius.Adapter.Core;
using MultiFactor.Radius.Adapter.Core.Http;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Core.Serialization;
using MultiFactor.Radius.Adapter.Server;
using MultiFactor.Radius.Adapter.Server.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Services.MultiFactorApi
{
    /// <summary>
    /// Service to interact with multifactor web api
    /// </summary>
    public class MultiFactorApiClient : IMultiFactorApiClient
    {
        private readonly IAuthenticatedClientCache _authenticatedClientCache;
        private ILogger<MultiFactorApiClient> _logger;
        private readonly IHttpClientAdapter _httpClientAdapter;

        public MultiFactorApiClient(IAuthenticatedClientCache authenticatedClientCache, ILogger<MultiFactorApiClient> logger, IHttpClientAdapter httpClientAdapter)
        {
            _authenticatedClientCache = authenticatedClientCache ?? throw new ArgumentNullException(nameof(authenticatedClientCache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClientAdapter = httpClientAdapter ?? throw new ArgumentNullException(nameof(httpClientAdapter));
        }

        public async Task<PacketCode> CreateSecondFactorRequest(RadiusContext context)
        {
            var userName = context.SecondFactorIdentity;
            var displayName = context.DisplayName;
            var email = context.EmailAddress;
            var userPhone = context.UserPhone;
            var callingStationId = context.RequestPacket.CallingStationId;

            string calledStationId = context.RequestPacket.CalledStationId; //only for winlogon yet

            if (string.IsNullOrEmpty(userName))
            {
                _logger.LogWarning("Empty user name for second factor request. Request rejected.");
                return PacketCode.AccessReject;
            }

            //remove user information for privacy
            switch (context.ClientConfiguration.PrivacyMode.Mode)
            {
                case PrivacyMode.Full:
                    displayName = null;
                    email = null;
                    userPhone = null;
                    callingStationId = "";
                    calledStationId = null;
                    break;

                case PrivacyMode.Partial:
                    if (!context.ClientConfiguration.PrivacyMode.HasField("Name"))
                    {
                        displayName = null;
                    }

                    if (!context.ClientConfiguration.PrivacyMode.HasField("Email"))
                    {
                        email = null;
                    }

                    if (!context.ClientConfiguration.PrivacyMode.HasField("Phone"))
                    {
                        userPhone = null;
                    }

                    if (!context.ClientConfiguration.PrivacyMode.HasField("RemoteHost"))
                    {
                        callingStationId = "";
                    }

                    calledStationId = null;

                    break;
            }

            //try to get authenticated client to bypass second factor if configured
            if (_authenticatedClientCache.TryHitCache(context.RequestPacket.CallingStationId, userName, context.ClientConfiguration))
            {
                _logger.LogInformation("Bypass second factor for user '{user:l}' with calling-station-id {csi:l} from {host:l}:{port}",
                    userName, context.RequestPacket.CallingStationId, context.RemoteEndpoint.Address, context.RemoteEndpoint.Port);
                return PacketCode.AccessAccept;
            }

            var payload = new
            {
                Identity = userName,
                Name = displayName,
                Email = email,
                Phone = userPhone,
                PassCode = GetPassCodeOrNull(context),
                CallingStationId = callingStationId,
                CalledStationId = calledStationId,
                Capabilities = new
                {
                    InlineEnroll = true
                },
                GroupPolicyPreset = new
                {
                    context.ClientConfiguration.SignUpGroups
                }
            };

            try
            {
                var response = await SendRequest("access/requests/ra", payload, context.ClientConfiguration);
                var responseCode = ConvertToRadiusCode(response);

                context.State = response?.Id;
                context.ReplyMessage = response?.ReplyMessage;

                if (responseCode == PacketCode.AccessAccept && !response.Bypassed)
                {
                    LogGrantedInfo(userName, response, context);
                    _authenticatedClientCache.SetCache(context.RequestPacket.CallingStationId, userName, context.ClientConfiguration);
                }

                if (responseCode == PacketCode.AccessReject)
                {
                    var reason = response?.ReplyMessage;
                    var phone = response?.Phone;
                    _logger.LogWarning("Second factor verification for user '{user:l}' from {host:l}:{port} failed with reason='{reason:l}'. User phone {phone:l}", userName, context.RemoteEndpoint.Address, context.RemoteEndpoint.Port, reason, phone);
                }

                return responseCode;
            }
            catch (Exception ex)
            {
                return HandleException(ex, userName, context);
            }
        }

        public async Task<PacketCode> Challenge(RadiusContext context, string answer, ChallengeRequestIdentifier identifier)
        {
            var userName = context.SecondFactorIdentity;
            var payload = new
            {
                Identity = userName,
                Challenge = answer,
                RequestId = identifier.RequestId
            };

            try
            {
                var response = await SendRequest("access/requests/ra/challenge", payload, context.ClientConfiguration);
                var responseCode = ConvertToRadiusCode(response);

                context.ReplyMessage = response.ReplyMessage;

                if (responseCode == PacketCode.AccessAccept && !response.Bypassed)
                {
                    LogGrantedInfo(userName, response, context);
                    _authenticatedClientCache.SetCache(context.RequestPacket.CallingStationId, userName, context.ClientConfiguration);
                }

                return responseCode;
            }
            catch (Exception ex)
            {
                return HandleException(ex, userName, context);
            }
        }

        private async Task<MultiFactorAccessRequest> SendRequest(string url, object payload, IClientConfiguration clientConfiguration)
        {
            var headers = new Dictionary<string, string>
            {
                {"Authorization", $"Basic {clientConfiguration.ApiCredential.GetHttpBasicAuthorizationHeaderValue()}" }
            };

            try
            {
                var response = await _httpClientAdapter.PostAsync<MultiFactorApiResponse<MultiFactorAccessRequest>>(url, payload, headers);
                if (!response.Success)
                {
                    _logger.LogWarning("Got unsuccessful response from API: {@response}", response);
                }

                return response.Model;
            }
            catch (TaskCanceledException ex)
            {
                var message = ex is TaskCanceledException ? "Timed out" : ex.Message;
                var err = $"Multifactor API host unreachable: {url}. Reason: {message}";
                throw new MultifactorApiUnreachableException(err, ex);
            }
            catch (Exception ex)
            {
                var err = $"Multifactor API host unreachable: {url}. Reason: {ex.Message}";
                throw new MultifactorApiUnreachableException(err, ex);
            }
        }

        private PacketCode HandleException(Exception ex, string username, RadiusContext context)
        {
            if (ex is MultifactorApiUnreachableException apiEx)
            {
                _logger.LogError("Error occured while requesting API for user '{user:l}' from {host:l}:{port}, {msg:l}",
                    username,
                    context.RemoteEndpoint.Address,
                    context.RemoteEndpoint.Port,
                    apiEx.Message);

                if (context.ClientConfiguration.BypassSecondFactorWhenApiUnreachable)
                {
                    _logger.LogWarning("Bypass second factor for user '{user:l}' from {host:l}:{port}",
                        username,
                        context.RemoteEndpoint.Address,
                        context.RemoteEndpoint.Port);
                    return ConvertToRadiusCode(MultiFactorAccessRequest.Bypass);
                }
            }

            return ConvertToRadiusCode(null);
        }

        private PacketCode ConvertToRadiusCode(MultiFactorAccessRequest multifactorAccessRequest)
        {
            if (multifactorAccessRequest == null)
            {
                return PacketCode.AccessReject;
            }

            switch (multifactorAccessRequest.Status)
            {
                case Literals.RadiusCode.Granted:     //authenticated by push
                    return PacketCode.AccessAccept;
                case Literals.RadiusCode.Denied:
                    return PacketCode.AccessReject; //access denied
                case Literals.RadiusCode.AwaitingAuthentication:
                    return PacketCode.AccessChallenge;  //otp code required
                default:
                    _logger.LogWarning($"Got unexpected status from API: {multifactorAccessRequest.Status}");
                    return PacketCode.AccessReject; //access denied
            }
        }

        private string GetPassCodeOrNull(RadiusContext context)
        {
            //check static challenge
            var challenge = context.RequestPacket.TryGetChallenge();
            if (challenge != null)
            {
                return challenge;
            }

            //check password challenge (otp or passcode)
            var userPassword = context.RequestPacket.TryGetUserPassword();

            //only if first authentication factor is None, assuming that Password contains OTP code
            if (context.ClientConfiguration.FirstFactorAuthenticationSource != AuthenticationSource.None)
            {
                return null;
            }

            /* valid passcodes:
             *  6 digits: otp
             *  t: Telegram
             *  m: MobileApp
             *  s: SMS
             *  c: PhoneCall
             */

            if (string.IsNullOrEmpty(userPassword))
            {
                return null;
            }

            var isOtp = Regex.IsMatch(userPassword.Trim(), "^[0-9]{1,6}$");
            if (isOtp)
            {
                return userPassword.Trim();
            }

            if (new[] { "t", "m", "s", "c" }.Any(c => c == userPassword.Trim().ToLower()))
            {
                return userPassword.Trim().ToLower();
            }

            //not a passcode
            return null;
        }

        private void LogGrantedInfo(string userName, MultiFactorAccessRequest response, RadiusContext context)
        {
            string countryValue = null;
            string regionValue = null;
            string cityValue = null;
            string callingStationId = context?.RequestPacket?.CallingStationId;

            if (response != null && IPAddress.TryParse(callingStationId, out var ip))
            {
                countryValue = response.CountryCode;
                regionValue = response.Region;
                cityValue = response.City;
                callingStationId = ip.ToString();
            }

            _logger.LogInformation("Second factor for user '{user:l}' verified successfully. Authenticator: '{authenticator:l}', account: '{account:l}', country: '{country:l}', region: '{region:l}', city: '{city:l}', calling-station-id: {clientIp}, authenticatorId: {authenticatorId}",
                        userName,
                        response?.Authenticator,
                        response?.Account,
                        countryValue,
                        regionValue,
                        cityValue,
                        callingStationId,
                        response.AuthenticatorId);
        }
    }
}
