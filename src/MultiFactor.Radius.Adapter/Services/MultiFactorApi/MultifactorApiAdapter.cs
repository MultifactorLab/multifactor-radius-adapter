﻿//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md


using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Configuration.Features.PreAuthModeFeature;
using MultiFactor.Radius.Adapter.Configuration.Features.PrivacyModeFeature;
using MultiFactor.Radius.Adapter.Core.Http;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Framework.Context;
using MultiFactor.Radius.Adapter.Server;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi.Dto;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi.Models;
using System;
using System.Net;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Services.MultiFactorApi
{
    internal class MultifactorApiAdapter : IMultifactorApiAdapter
    {
        private readonly IMultifactorApiClient _api;
        private readonly IServiceConfiguration _serviceConfiguration;
        private readonly IAuthenticatedClientCache _authenticatedClientCache;
        private readonly ILogger<MultifactorApiAdapter> _logger;

        public MultifactorApiAdapter(IMultifactorApiClient api,
            IServiceConfiguration serviceConfiguration,
            IAuthenticatedClientCache authenticatedClientCache,
            ILogger<MultifactorApiAdapter> logger)
        {
            _api = api;
            _serviceConfiguration = serviceConfiguration;
            _authenticatedClientCache = authenticatedClientCache;
            _logger = logger;
        }

        public async Task<SecondFactorResponse> CreateSecondFactorRequestAsync(RadiusContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (string.IsNullOrEmpty(context.SecondFactorIdentity))
            {
                _logger.LogWarning("Empty user name for second factor request. Request rejected.");
                return new SecondFactorResponse(PacketCode.AccessReject);
            }

            var identity = context.SecondFactorIdentity;
            var displayName = context.DisplayName;
            var email = context.EmailAddress;
            var userPhone = context.UserPhone;
            var callingStationId = context.RequestPacket.CallingStationId;
            var calledStationId = context.RequestPacket.CalledStationId; //only for winlogon yet

            //remove user information for privacy
            switch (context.Configuration.PrivacyMode.Mode)
            {
                case PrivacyMode.Full:
                    displayName = null;
                    email = null;
                    userPhone = null;
                    callingStationId = "";
                    calledStationId = null;
                    break;

                case PrivacyMode.Partial:
                    if (!context.Configuration.PrivacyMode.HasField("Name"))
                    {
                        displayName = null;
                    }

                    if (!context.Configuration.PrivacyMode.HasField("Email"))
                    {
                        email = null;
                    }

                    if (!context.Configuration.PrivacyMode.HasField("Phone"))
                    {
                        userPhone = null;
                    }

                    if (!context.Configuration.PrivacyMode.HasField("RemoteHost"))
                    {
                        callingStationId = "";
                    }

                    calledStationId = null;

                    break;
            }

            //try to get authenticated client to bypass second factor if configured
            if (_authenticatedClientCache.TryHitCache(context.RequestPacket.CallingStationId, identity, context.Configuration))
            {
                _logger.LogInformation("Bypass second factor for user '{user:l}' with calling-station-id {csi:l} from {host:l}:{port}",
                    identity,
                    context.RequestPacket.CallingStationId,
                    context.RemoteEndpoint.Address,
                    context.RemoteEndpoint.Port);
                return new SecondFactorResponse(PacketCode.AccessAccept);
            }

            var payload = new CreateRequestDto
            {
                Identity = identity,
                Name = displayName,
                Email = email,
                Phone = userPhone,
                PassCode = GetPassCodeOrNull(context),
                CallingStationId = callingStationId,
                CalledStationId = calledStationId,
                Capabilities = new CapabilitiesDto
                {
                    InlineEnroll = true
                },
                GroupPolicyPreset = new GroupPolicyPresetDto
                {
                    SignUpGroups = context.Configuration.SignUpGroups
                }
            };

            try
            {
                var cred = context.Configuration.ApiCredential;
                var auth = new BasicAuthHeaderValue(cred.Usr, cred.Pwd);

                var response = await _api.CreateRequestAsync(payload, auth);
                var responseCode = ConvertToRadiusCode(response);
                if (responseCode == PacketCode.AccessAccept && !response.Bypassed)
                {
                    LogGrantedInfo(identity, response, context);
                    _authenticatedClientCache.SetCache(context.RequestPacket.CallingStationId, identity, context.Configuration);
                }

                if (responseCode == PacketCode.AccessReject)
                {
                    var reason = response?.ReplyMessage;
                    var phone = response?.Phone;
                    _logger.LogWarning("Second factor verification for user '{user:l}' from {host:l}:{port} failed with reason='{reason:l}'. User phone {phone:l}",
                        identity,
                        context.RemoteEndpoint.Address,
                        context.RemoteEndpoint.Port,
                        reason,
                        phone);
                }

                return new SecondFactorResponse(responseCode, response?.Id, response?.ReplyMessage);
            }
            catch (MultifactorApiUnreachableException apiEx)
            {
                _logger.LogError(apiEx, "Error occured while requesting API for user '{user:l}' from {host:l}:{port}, {msg:l}",
                    identity,
                    context.RemoteEndpoint.Address,
                    context.RemoteEndpoint.Port,
                    apiEx.Message);

                if (!context.Configuration.BypassSecondFactorWhenApiUnreachable)
                {
                    var radCode = ConvertToRadiusCode(null);
                    return new SecondFactorResponse(radCode);
                }

                _logger.LogWarning("Bypass second factor for user '{user:l}' from {host:l}:{port}",
                    identity,
                    context.RemoteEndpoint.Address,
                    context.RemoteEndpoint.Port);

                var code = ConvertToRadiusCode(AccessRequestDto.Bypass);
                return new SecondFactorResponse(code);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occured while requesting API for user '{user:l}' from {host:l}:{port}, {msg:l}",
                    identity,
                    context.RemoteEndpoint.Address,
                    context.RemoteEndpoint.Port,
                    ex.Message);

                var code = ConvertToRadiusCode(null);
                return new SecondFactorResponse(code);
            }
        }

        public async Task<ChallengeResponse> ChallengeAsync(RadiusContext context, string answer, ChallengeRequestIdentifier identifier)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (identifier is null)
            {
                throw new ArgumentNullException(nameof(identifier));
            }

            var identity = context.SecondFactorIdentity;
            var payload = new ChallengeDto
            {
                Identity = identity,
                Challenge = answer,
                RequestId = identifier.RequestId
            };

            try
            {
                var cred = context.Configuration.ApiCredential;
                var auth = new BasicAuthHeaderValue(cred.Usr, cred.Pwd);

                var response = await _api.ChallengeAsync(payload, auth);
                var responseCode = ConvertToRadiusCode(response);
                if (responseCode == PacketCode.AccessAccept && !response.Bypassed)
                {
                    LogGrantedInfo(identity, response, context);
                    _authenticatedClientCache.SetCache(context.RequestPacket.CallingStationId, identity, context.Configuration);
                }

                return new ChallengeResponse(responseCode, response?.ReplyMessage);
            }
            catch (MultifactorApiUnreachableException apiEx)
            {
                _logger.LogError(apiEx, "Error occured while requesting API for user '{user:l}' from {host:l}:{port}, {msg:l}",
                    identity,
                    context.RemoteEndpoint.Address,
                    context.RemoteEndpoint.Port,
                    apiEx.Message);

                if (!context.Configuration.BypassSecondFactorWhenApiUnreachable)
                {
                    var radCode = ConvertToRadiusCode(null);
                    return new ChallengeResponse(radCode);
                }

                _logger.LogWarning("Bypass second factor for user '{user:l}' from {host:l}:{port}",
                        identity,
                        context.RemoteEndpoint.Address,
                        context.RemoteEndpoint.Port);
                var code = ConvertToRadiusCode(AccessRequestDto.Bypass);

                return new ChallengeResponse(code);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occured while requesting API for user '{user:l}' from {host:l}:{port}, {msg:l}",
                    identity,
                    context.RemoteEndpoint.Address,
                    context.RemoteEndpoint.Port,
                    ex.Message);

                var code = ConvertToRadiusCode(null);
                return new ChallengeResponse(code);
            }
        }

        private static string GetPassCodeOrNull(RadiusContext context)
        {
            //check static challenge
            var challenge = context.RequestPacket.TryGetChallenge();
            if (challenge != null)
            {
                return challenge;
            }

            //check password challenge (otp or passcode)
            var passphrase = context.Passphrase;
            switch (context.Configuration.PreAuthnMode.Mode)
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

            if (context.FirstFactorAuthenticationSource != AuthenticationSource.None)
            {
                return null;
            }

            return context.Passphrase.Otp ?? passphrase.ProviderCode;
        }

        private PacketCode ConvertToRadiusCode(AccessRequestDto multifactorAccessRequest)
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

        private void LogGrantedInfo(string identity, AccessRequestDto response, RadiusContext context)
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
                        identity,
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