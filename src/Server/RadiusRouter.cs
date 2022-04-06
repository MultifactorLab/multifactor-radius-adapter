//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Core;
using MultiFactor.Radius.Adapter.Services;
using MultiFactor.Radius.Adapter.Services.Ldap;
using Serilog;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Server
{
    /// <summary>
    /// Main processor
    /// </summary>
    public class RadiusRouter
    {
        private ServiceConfiguration _serviceConfiguration;
        private ILogger _logger;
        private IRadiusPacketParser _packetParser;
        private MultiFactorApiClient _multifactorApiClient;
        public event EventHandler<PendingRequest> RequestProcessed;
        private readonly ConcurrentDictionary<string, PendingRequest> _stateChallengePendingRequests = new ConcurrentDictionary<string, PendingRequest>();

        private DateTime _startedAt = DateTime.Now;

        public RadiusRouter(ServiceConfiguration serviceConfiguration, IRadiusPacketParser packetParser, ILogger logger)
        {
            _serviceConfiguration = serviceConfiguration ?? throw new ArgumentNullException(nameof(serviceConfiguration));
            _packetParser = packetParser ?? throw new ArgumentNullException(nameof(packetParser));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _multifactorApiClient = new MultiFactorApiClient(serviceConfiguration, logger);
        }

        public async Task HandleRequest(PendingRequest request, ClientConfiguration clientConfig)
        {
            try
            {
                if (request.RequestPacket.Code == PacketCode.StatusServer)
                {
                    //status
                    var uptime = (DateTime.Now - _startedAt);
                    request.ReplyMessage = $"Server up {uptime.Days} days {uptime.ToString("hh\\:mm\\:ss")}";
                    request.ResponseCode = PacketCode.AccessAccept;
                    RequestProcessed?.Invoke(this, request);
                    return;
                }

                if (request.RequestPacket.Code != PacketCode.AccessRequest)
                {
                    _logger.Warning("Unprocessable packet type: {code}", request.RequestPacket.Code);
                    return;
                }

                if (request.RequestPacket.Attributes.ContainsKey("State")) //Access-Challenge response 
                {
                    var receivedState = request.RequestPacket.GetString("State");

                    if (_stateChallengePendingRequests.ContainsKey(receivedState))
                    {
                        //second request with Multifactor challenge
                        request.ResponseCode = await ProcessChallenge(request, clientConfig, receivedState);
                        request.State = receivedState;  //state for Access-Challenge message if otp is wrong (3 times allowed)

                        RequestProcessed?.Invoke(this, request);
                        return; //stop authentication process after otp code verification
                    }
                }

                var firstFactorAuthenticationResultCode = await ProcessFirstAuthenticationFactor(request, clientConfig);
                if (firstFactorAuthenticationResultCode != PacketCode.AccessAccept)
                {
                    //first factor authentication rejected
                    request.ResponseCode = firstFactorAuthenticationResultCode;
                    RequestProcessed?.Invoke(this, request);

                    //stop authencation process
                    return;
                }

                if (request.Bypass2Fa)
                {
                    //second factor not trquired
                    var userName = request.RequestPacket.UserName;
                    _logger.Information("Bypass second factor for user '{user:l}'", userName);

                    request.ResponseCode = PacketCode.AccessAccept;
                    RequestProcessed?.Invoke(this, request);

                    //stop authencation process
                    return;
                }

                var secondFactorAuthenticationResultCode = await ProcessSecondAuthenticationFactor(request, clientConfig);

                request.ResponseCode = secondFactorAuthenticationResultCode;

                if (request.ResponseCode == PacketCode.AccessChallenge)
                {
                    AddStateChallengePendingRequest(request.State, request);
                }

                RequestProcessed?.Invoke(this, request);

            }
            catch(Exception ex)
            {
                _logger.Error(ex, "HandleRequest");
            }
        }

        private async Task<PacketCode> ProcessFirstAuthenticationFactor(PendingRequest request, ClientConfiguration clientConfig)
        {
            switch(clientConfig.FirstFactorAuthenticationSource)
            {
                case AuthenticationSource.ActiveDirectory:  //AD auth
                case AuthenticationSource.Ldap:             //LDAP auth
                    return await ProcessLdapAuthentication(request, clientConfig);
                case AuthenticationSource.Radius:           //RADIUS auth
                    return await ProcessRadiusAuthentication(request, clientConfig);
                case AuthenticationSource.None:
                    return PacketCode.AccessAccept;         //first factor not required
                default:                                    //unknown source
                    throw new NotImplementedException(clientConfig.FirstFactorAuthenticationSource.ToString());
            }
        }

        /// <summary>
        /// Authenticate request at LDAP/Active Directory Domain with user-name and password
        /// </summary>
        private async Task<PacketCode> ProcessLdapAuthentication(PendingRequest request, ClientConfiguration clientConfig)
        {
            var userName = request.RequestPacket.UserName;
            var password = request.RequestPacket.UserPassword;

            if (string.IsNullOrEmpty(userName))
            {
                _logger.Warning("Can't find User-Name in message id={id} from {host:l}:{port}", request.RequestPacket.Identifier, request.RemoteEndpoint.Address, request.RemoteEndpoint.Port);
                return PacketCode.AccessReject;
            }

            if (string.IsNullOrEmpty(password))
            {
                _logger.Warning("Can't find User-Password in message id={id} from {host:l}:{port}", request.RequestPacket.Identifier, request.RemoteEndpoint.Address, request.RemoteEndpoint.Port);
                return PacketCode.AccessReject;
            }

            LdapService _service;
            switch (clientConfig.FirstFactorAuthenticationSource)
            {
                case AuthenticationSource.ActiveDirectory:
                    _service = new ActiveDirectoryService(_serviceConfiguration, _logger);
                    break;
                case AuthenticationSource.Ldap:
                    _service = new LdapService(_serviceConfiguration, _logger);
                    break;
                default:
                    throw new NotImplementedException(clientConfig.FirstFactorAuthenticationSource.ToString());
            }

            //check all hosts
            var ldapUriList = clientConfig.ActiveDirectoryDomain.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach(var ldapUri in ldapUriList)
            {
                var isValid = await _service.VerifyCredential(userName, password, ldapUri, request, clientConfig);
                if (isValid)
                {
                    return PacketCode.AccessAccept;
                }
            }

            return PacketCode.AccessReject;
        }

        /// <summary>
        /// Authenticate request at Remote Radius Server with user-name and password
        /// </summary>
        private async Task<PacketCode> ProcessRadiusAuthentication(PendingRequest request, ClientConfiguration clientConfig)
        {
            try
            {
                //sending request as is to Remote Radius Server
                using (var client = new RadiusClient(clientConfig.ServiceClientEndpoint, _logger))
                {
                    _logger.Debug($"Sending AccessRequest message with id={{id}} to Remote Radius Server {clientConfig.NpsServerEndpoint}", request.RequestPacket.Identifier);

                    var requestBytes = _packetParser.GetBytes(request.RequestPacket);
                    var response = await client.SendPacketAsync(request.RequestPacket.Identifier, requestBytes, clientConfig.NpsServerEndpoint, TimeSpan.FromSeconds(5));

                    if (response != null)
                    {
                        var responsePacket = _packetParser.Parse(response, request.RequestPacket.SharedSecret, request.RequestPacket.Authenticator);
                        _logger.Debug("Received {code:l} message with id={id} from Remote Radius Server", responsePacket.Code.ToString(), responsePacket.Identifier);
                        
                        if (responsePacket.Code == PacketCode.AccessAccept)
                        {
                            var userName = request.RequestPacket.UserName;
                            _logger.Information($"User '{{user:l}}' credential and status verified successfully at {clientConfig.NpsServerEndpoint}", userName);
                        }

                        request.ResponsePacket = responsePacket;
                        return responsePacket.Code; //Code received from remote radius
                    }
                    else
                    {
                        _logger.Warning("Remote Radius Server did not respond on message with id={id}", request.RequestPacket.Identifier);
                        return PacketCode.AccessReject; //reject by default
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Radius authentication error");
            }
            
            return PacketCode.AccessReject; //reject by default
        }

        /// <summary>
        /// Authenticate request at MultiFactor with user-name only
        /// </summary>
        private async Task<PacketCode> ProcessSecondAuthenticationFactor(PendingRequest request, ClientConfiguration clientConfig)
        {
            var userName = request.RequestPacket.UserName;

            if (string.IsNullOrEmpty(userName))
            {
                _logger.Warning("Can't find User-Name in message id={id} from {host:l}:{port}", request.RequestPacket.Identifier, request.RemoteEndpoint.Address, request.RemoteEndpoint.Port);
                return PacketCode.AccessReject;
            }

            if (request.RequestPacket.IsVendorAclRequest == true)
            {
                //security check
                if (clientConfig.FirstFactorAuthenticationSource == AuthenticationSource.Radius)
                {
                    _logger.Information("Bypass second factor for user {user:l}", userName);
                    return PacketCode.AccessAccept;
                }
            }

            var response = await _multifactorApiClient.CreateSecondFactorRequest(request, clientConfig);

            return response;
        }

        /// <summary>
        /// Verify one time password from user input
        /// </summary>
        private async Task<PacketCode> ProcessChallenge(PendingRequest request, ClientConfiguration clientConfig, string state)
        {
            var userName = request.RequestPacket.UserName;

            if (string.IsNullOrEmpty(userName))
            {
                _logger.Warning("Can't find User-Name in message id={id} from {host:l}:{port}", request.RequestPacket.Identifier, request.RemoteEndpoint.Address, request.RemoteEndpoint.Port);
                return PacketCode.AccessReject;
            }

            PacketCode response;
            string userAnswer;

            switch (request.RequestPacket.AuthenticationType)
            {
                case AuthenticationType.PAP:
                    //user-password attribute holds second request challenge from user
                    userAnswer = request.RequestPacket.GetString("User-Password");

                    if (string.IsNullOrEmpty(userAnswer))
                    {
                        _logger.Warning("Can't find User-Password with user response in message id={id} from {host:l}:{port}", request.RequestPacket.Identifier, request.RemoteEndpoint.Address, request.RemoteEndpoint.Port);
                        return PacketCode.AccessReject;
                    }

                    break;
                case AuthenticationType.MSCHAP2:
                    var msChapResponse = request.RequestPacket.GetAttribute<byte[]>("MS-CHAP2-Response");

                    if (msChapResponse == null)
                    {
                        _logger.Warning("Can't find MS-CHAP2-Response in message id={id} from {host:l}:{port}", request.RequestPacket.Identifier, request.RemoteEndpoint.Address, request.RemoteEndpoint.Port);
                        return PacketCode.AccessReject;
                    }

                    //forti behaviour
                    var otpData = msChapResponse.Skip(2).Take(6).ToArray();
                    userAnswer = Encoding.ASCII.GetString(otpData);

                    break;
                default:
                    _logger.Warning("Unable to process {auth} challange in message id={id} from {host:l}:{port}", request.RequestPacket.AuthenticationType, request.RequestPacket.Identifier, request.RemoteEndpoint.Address, request.RemoteEndpoint.Port);
                    return PacketCode.AccessReject;
            }

            response = await _multifactorApiClient.Challenge(request, clientConfig, userName, userAnswer, state);

            switch (response)
            {
                case PacketCode.AccessAccept:
                    var stateChallengePendingRequest = GetStateChallengeRequest(state);
                    if (stateChallengePendingRequest != null)
                    {
                        request.UserGroups = stateChallengePendingRequest.UserGroups;
                        request.ResponsePacket = stateChallengePendingRequest.ResponsePacket;
                        request.LdapAttrs = stateChallengePendingRequest.LdapAttrs;
                    }
                    break;
                case PacketCode.AccessReject:
                    RemoveStateChallengeRequest(state);
                    break;
            }

            return response;
        }

        /// <summary>
        /// Add authenticated request to local cache for otp/challenge
        /// </summary>
        private void AddStateChallengePendingRequest(string state, PendingRequest request)
        {
            if (!_stateChallengePendingRequests.TryAdd(state, request))
            {
                _logger.Error("Unable to cache request id={id}", request.RequestPacket.Identifier);
            }
        }

        /// <summary>
        /// Get authenticated request from local cache for otp/challenge
        /// </summary>
        private PendingRequest GetStateChallengeRequest(string state)
        {
            if (_stateChallengePendingRequests.TryRemove(state, out PendingRequest request))
            {
                return request;
            }

            _logger.Error($"Unable to get cached request with state={state}");
            return null;
        }

        /// <summary>
        /// Remove authenticated request from local cache
        /// </summary>
        /// <param name="state"></param>
        private void RemoveStateChallengeRequest(string state)
        {
            _stateChallengePendingRequests.TryRemove(state, out PendingRequest _);
        }
    }
}