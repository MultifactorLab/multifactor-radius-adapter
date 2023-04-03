//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md


using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Configuration.Features.PrivacyModeFeature;
using MultiFactor.Radius.Adapter.Core;
using MultiFactor.Radius.Adapter.Core.Http;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Core.Serialization;
using MultiFactor.Radius.Adapter.Server;
using Serilog;
using System;
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
    public class MultiFactorApiClient
    {
        private IServiceConfiguration _serviceConfiguration;
        private readonly AuthenticatedClientCache _authenticatedClientCache;
        private ILogger _logger;

        public MultiFactorApiClient(IServiceConfiguration serviceConfiguration, AuthenticatedClientCache authenticatedClientCache, ILogger logger)
        {
            _serviceConfiguration = serviceConfiguration ?? throw new ArgumentNullException(nameof(serviceConfiguration));
            _authenticatedClientCache = authenticatedClientCache ?? throw new ArgumentNullException(nameof(authenticatedClientCache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PacketCode> CreateSecondFactorRequest(PendingRequest request, IClientConfiguration clientConfig)
        {
            var userName = request.UserName;
            var displayName = request.DisplayName;
            var email = request.EmailAddress;
            var userPhone = request.UserPhone;
            var callingStationId = request.RequestPacket.CallingStationId;

            string calledStationId = null;

            if (request.RequestPacket.IsWinLogon) //only for winlogon yet
            {
                calledStationId = request.RequestPacket.CalledStationId;
            }

            if (clientConfig.UseUpnAsIdentity)
            {
                if (string.IsNullOrEmpty(request.Upn))
                {
                    throw new ArgumentNullException("UserPrincipalName");
                }

                userName = request.Upn;
            }

            //remove user information for privacy
            switch (clientConfig.PrivacyMode.Mode)
            {
                case PrivacyMode.Full:
                    displayName = null;
                    email = null;
                    userPhone = null;
                    callingStationId = "";
                    calledStationId = null;
                    break;

                case PrivacyMode.Partial:
                    if (!clientConfig.PrivacyMode.HasField("Name"))
                    {
                        displayName = null;
                    }
                    
                    if (!clientConfig.PrivacyMode.HasField("Email"))
                    {
                        email = null;
                    }
                    
                    if (!clientConfig.PrivacyMode.HasField("Phone"))
                    {
                        userPhone = null;
                    }
                    
                    if (!clientConfig.PrivacyMode.HasField("RemoteHost"))
                    {
                        callingStationId = "";
                    }

                    calledStationId = null;

                    break;
            }

            //try to get authenticated client to bypass second factor if configured
            if (_authenticatedClientCache.TryHitCache(request.RequestPacket.CallingStationId, userName, clientConfig))
            {
                _logger.Information("Bypass second factor for user '{user:l}' with calling-station-id {csi:l} from {host:l}:{port}", 
                    userName, request.RequestPacket.CallingStationId, request.RemoteEndpoint.Address, request.RemoteEndpoint.Port);
                return PacketCode.AccessAccept;
            }
            
            var url = _serviceConfiguration.ApiUrl + "/access/requests/ra";
            var payload = new
            {
                Identity = userName,
                Name = displayName,
                Email = email,
                Phone = userPhone,
                PassCode = GetPassCodeOrNull(request, clientConfig),
                CallingStationId = callingStationId,
                CalledStationId = calledStationId,
                Capabilities = new
                {
                    InlineEnroll = true
                },
                GroupPolicyPreset = new
                {
                    clientConfig.SignUpGroups
                }
            };

            try
            {
                var response = await SendRequest(url, payload, clientConfig);
                var responseCode = ConvertToRadiusCode(response);

                request.State = response?.Id;
                request.ReplyMessage = response?.ReplyMessage;

                if (responseCode == PacketCode.AccessAccept && !response.Bypassed)
                {
                    LogGrantedInfo(userName, response, request);
                    _authenticatedClientCache.SetCache(request.RequestPacket.CallingStationId, userName, clientConfig);                  
                }

                if (responseCode == PacketCode.AccessReject)
                {
                    var reason = response?.ReplyMessage;
                    var phone = response?.Phone;
                    _logger.Warning("Second factor verification for user '{user:l}' from {host:l}:{port} failed with reason='{reason:l}'. User phone {phone:l}", userName, request.RemoteEndpoint.Address, request.RemoteEndpoint.Port, reason, phone);
                }

                return responseCode;
            }
            catch (Exception ex)
            {
                return HandleException(ex, userName, request, clientConfig);
            }
        }

        public async Task<PacketCode> Challenge(PendingRequest request, IClientConfiguration clientConfig, string userName, string answer, string state)
        {
            var url = _serviceConfiguration.ApiUrl + "/access/requests/ra/challenge";
            var payload = new
            {
                Identity = userName,
                Challenge = answer,
                RequestId = state
            };

            try
            {
                var response = await SendRequest(url, payload, clientConfig);
                var responseCode = ConvertToRadiusCode(response);

                request.ReplyMessage = response.ReplyMessage;

                if (responseCode == PacketCode.AccessAccept && !response.Bypassed)
                {
                    LogGrantedInfo(userName, response, request);
                    _authenticatedClientCache.SetCache(request.RequestPacket.CallingStationId, userName, clientConfig);
                }

                return responseCode;
            }
            catch (Exception ex)
            {
                return HandleException(ex, userName, request, clientConfig);
            }
        }

        private async Task<MultiFactorAccessRequest> SendRequest(string url, object payload, IClientConfiguration clientConfiguration)
        {
            try
            {
                //make sure we can communicate securely
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                ServicePointManager.DefaultConnectionLimit = 100;

                _logger.Debug("Sending request to API: {@payload}", payload);

                var json = JsonSerializer.Serialize(payload, SerializerOptions.JsonSerializerOptions);
                var requestData = Encoding.UTF8.GetBytes(json);
                byte[] responseData = null;

                //basic authorization
                var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{clientConfiguration.MultifactorApiKey}:{clientConfiguration.MultiFactorApiSecret}"));

                using (var web = new WebClient())
                {
                    web.Headers.Add("Content-Type", "application/json");
                    web.Headers.Add("Authorization", $"Basic {auth}");

                    if (!string.IsNullOrEmpty(_serviceConfiguration.ApiProxy))
                    {
                        _logger.Debug("Using proxy {p:l}", _serviceConfiguration.ApiProxy);
                        if (!WebProxyFactory.TryCreateWebProxy(_serviceConfiguration.ApiProxy, out var webProxy))
                        {
                            _logger.Error("Unable to initialize WebProxy: '{pr:l}'", _serviceConfiguration.ApiProxy);
                            throw new Exception($"Unable to initialize WebProxy. Please, check whether '{Literals.Configuration.MultifactorApiProxy}' URI is valid.");
                        }
                        web.Proxy = webProxy;
                    }

                    responseData = await web.UploadDataTaskAsync(url, "POST", requestData);
                }

                json = Encoding.UTF8.GetString(responseData);
                var response = JsonSerializer.Deserialize<MultiFactorApiResponse<MultiFactorAccessRequest>>(json, SerializerOptions.JsonSerializerOptions);

                _logger.Debug("Received response from API: {@response}", response);

                if (!response.Success)
                {
                    _logger.Warning("Got unsuccessful response from API: {@response}", response);
                }

                return response.Model;
            }
            catch (Exception ex)
            {
                throw new MultifactorApiUnreachableException($"Multifactor API host unreachable: {url}. Reason: {ex.Message}", ex);
            }
        }

        private PacketCode HandleException(Exception ex, string username, PendingRequest request, IClientConfiguration clientConfig)
        {
            if (ex is MultifactorApiUnreachableException apiEx)
            {
                _logger.Error("Error occured while requesting API for user '{user:l}' from {host:l}:{port}, {msg:l}",
                    username,
                    request.RemoteEndpoint.Address,
                    request.RemoteEndpoint.Port,
                    apiEx.Message);

                if (clientConfig.BypassSecondFactorWhenApiUnreachable)
                {
                    _logger.Warning("Bypass second factor for user '{user:l}' from {host:l}:{port}",
                        username,
                        request.RemoteEndpoint.Address,
                        request.RemoteEndpoint.Port);
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
                case "Granted":     //authenticated by push
                    return PacketCode.AccessAccept;
                case "Denied":
                    return PacketCode.AccessReject; //access denied
                case "AwaitingAuthentication":
                    return PacketCode.AccessChallenge;  //otp code required
                default:
                    _logger.Warning($"Got unexpected status from API: {multifactorAccessRequest.Status}");
                    return PacketCode.AccessReject; //access denied
            }
        }

        private string GetPassCodeOrNull(PendingRequest request, IClientConfiguration clientConfiguration)
        {
            //check static challenge
            var challenge = request.RequestPacket.TryGetChallenge();
            if (challenge != null)
            {
                return challenge;
            }

            //check password challenge (otp or passcode)
            var userPassword = request.RequestPacket.TryGetUserPassword();

            //only if first authentication factor is None, assuming that Password contains OTP code
            if (clientConfiguration.FirstFactorAuthenticationSource != AuthenticationSource.None)
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

        private void LogGrantedInfo(string userName, MultiFactorAccessRequest response, PendingRequest request)
        {
            string countryValue = null;
            string regionValue = null;
            string cityValue = null;
            string callingStationId = request?.RequestPacket?.CallingStationId;

            if (response != null && IPAddress.TryParse(callingStationId, out var ip))
            {
                countryValue = response.CountryCode;
                regionValue = response.Region;
                cityValue = response.City;
                callingStationId = ip.ToString();
            }

            _logger.Information("Second factor for user '{user:l}' verified successfully. Authenticator: '{authenticator:l}', account: '{account:l}', country: '{country:l}', region: '{region:l}', city: '{city:l}', calling-station-id: {clientIp}, authenticatorId: {authenticatorId}",
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
