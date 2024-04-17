//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Configuration.Features.RandomWaiterFeature;
using MultiFactor.Radius.Adapter.Framework.Context;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Net;

namespace MultiFactor.Radius.Adapter.Configuration
{
    public class ServiceConfiguration : IServiceConfiguration
    {
        /// <summary>
        /// List of clients with identification by client ip
        /// </summary>
        private readonly IDictionary<IPAddress, IClientConfiguration> _ipClients = new Dictionary<IPAddress, IClientConfiguration>();

        /// <summary>
        /// List of clients with identification by NAS-Identifier attr
        /// </summary>
        private readonly IDictionary<string, IClientConfiguration> _nasIdClients = new Dictionary<string, IClientConfiguration>();

        private readonly List<IClientConfiguration> _clients = new();
        public ReadOnlyCollection<IClientConfiguration> Clients => _clients.AsReadOnly();

        public ServiceConfiguration() { }

        public IClientConfiguration GetClient(string nasIdentifier)
        {
            if (SingleClientMode)
            {
                return _ipClients[IPAddress.Any];
            }
            if (string.IsNullOrEmpty(nasIdentifier))
            {
                return null;
            }
            if (_nasIdClients.ContainsKey(nasIdentifier))
            {
                return _nasIdClients[nasIdentifier];
            }
            return null;
        }

        public IClientConfiguration GetClient(IPAddress ip)
        {
            if (SingleClientMode)
            {
                return _ipClients[IPAddress.Any];
            }
            if (_ipClients.ContainsKey(ip))
            {
                return _ipClients[ip];
            }
            return null;
        }

        public IClientConfiguration GetClient(RadiusContext request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (SingleClientMode)
            {
                return _ipClients[IPAddress.Any];
            }

            var nasId = request.RequestPacket.NasIdentifier;
            var ip = request.RemoteEndpoint.Address;

            return GetClient(nasId) ?? GetClient(ip);
        }

        /// <summary>
        /// This service RADIUS UDP Server endpoint
        /// </summary>
        public IPEndPoint ServiceServerEndpoint { get; private set; }

        /// <summary>
        /// Multifactor API URL
        /// </summary>
        public string ApiUrl { get; private set; }

        /// <summary>
        /// HTTP Proxy for API
        /// </summary>
        public string ApiProxy { get; private set; }

        /// <summary>
        /// HTTP timeout for Multifactor requests
        /// </summary>
        public TimeSpan ApiTimeout { get; private set; }

        public bool SingleClientMode { get; private set; }
        public RandomWaiterConfig InvalidCredentialDelay { get; private set; }

        public ServiceConfiguration SetApiProxy(string val)
        {
            if (string.IsNullOrWhiteSpace(val))
            {
                throw new ArgumentException($"'{nameof(val)}' cannot be null or whitespace.", nameof(val));
            }

            ApiProxy = val;
            return this;
        }

        public ServiceConfiguration SetApiUrl(string val)
        {
            if (string.IsNullOrWhiteSpace(val))
            {
                throw new ArgumentException($"'{nameof(val)}' cannot be null or whitespace.", nameof(val));
            }

            ApiUrl = val;
            return this;
        }

        public ServiceConfiguration SetApiTimeout(TimeSpan httpTimeoutSetting)
        {
            ApiTimeout = httpTimeoutSetting;
            return this;
        }

        public ServiceConfiguration AddClient(string nasId, IClientConfiguration client)
        {
            if (_nasIdClients.ContainsKey(nasId))
            {
                throw new ConfigurationErrorsException($"Client with NAS-Identifier '{nasId} already added from {_nasIdClients[nasId].Name}.config");
            }

            if (string.IsNullOrWhiteSpace(nasId))
            {
                throw new ArgumentException($"'{nameof(nasId)}' cannot be null or whitespace.", nameof(nasId));
            }

            if (client is null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            _nasIdClients.Add(nasId, client);
            _clients.Add(client);
            return this;
        }

        public ServiceConfiguration AddClient(IPAddress ip, IClientConfiguration client)
        {
            if (ip is null)
            {
                throw new ArgumentNullException(nameof(ip));
            }

            if (client is null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (_ipClients.ContainsKey(ip))
            {
                throw new ConfigurationErrorsException($"Client with IP {ip} already added from {_ipClients[ip].Name}.config");
            }

            _ipClients.Add(ip, client);
            _clients.Add(client);
            return this;
        }

        public ServiceConfiguration SetInvalidCredentialDelay(RandomWaiterConfig config)
        {
            InvalidCredentialDelay = config ?? throw new ArgumentNullException(nameof(config));
            return this;
        }

        public ServiceConfiguration SetServiceServerEndpoint(IPEndPoint endpoint)
        {
            ServiceServerEndpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            return this;
        }

        public ServiceConfiguration IsSingleClientMode(bool single)
        {
            SingleClientMode = single;
            return this;
        }
    }
}
