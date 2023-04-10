//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using Microsoft.AspNetCore.Mvc.Formatters;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Configuration.Features.RandomWaiterFeature;
using MultiFactor.Radius.Adapter.Core;
using MultiFactor.Radius.Adapter.Server;
using NetTools;
using Serilog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace MultiFactor.Radius.Adapter.Configuration
{
    public class ServiceConfiguration : IServiceConfiguration, IServiceConfigurationBuilder
    {
        /// <summary>
        /// List of clients with identification by client ip
        /// </summary>
        private readonly IDictionary<IPAddress, IClientConfiguration> _ipClients = new Dictionary<IPAddress, IClientConfiguration>();

        /// <summary>
        /// List of clients with identification by NAS-Identifier attr
        /// </summary>
        private readonly IDictionary<string, IClientConfiguration> _nasIdClients = new Dictionary<string, IClientConfiguration>();

        public IReadOnlyList<IClientConfiguration> Clients => _ipClients
            .Select(x => x.Value)
            .Concat(_nasIdClients.Select(x => x.Value))
            .ToList()
            .AsReadOnly();

        private ServiceConfiguration() { }

        public static IServiceConfigurationBuilder CreateBuilder()
        {
            return new ServiceConfiguration();
        }

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
                throw new ArgumentNullException("request");
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

        public bool SingleClientMode { get; private set; }
        public RandomWaiterConfig InvalidCredentialDelay { get; private set; }

        public IServiceConfigurationBuilder SetApiProxy(string val)
        {
            ApiProxy = val;
            return this;
        }

        public IServiceConfigurationBuilder SetApiUrl(string val)
        {
            ApiUrl = val;
            return this;
        }

        public IServiceConfigurationBuilder AddClient(string nasId, IClientConfiguration client)
        {
            if (_nasIdClients.ContainsKey(nasId))
            {
                throw new ConfigurationErrorsException($"Client with NAS-Identifier '{nasId} already added from {_nasIdClients[nasId].Name}.config");
            }

            _nasIdClients.Add(nasId, client);
            return this;
        }

        public IServiceConfigurationBuilder AddClient(IPAddress ip, IClientConfiguration client)
        {
            if (_ipClients.ContainsKey(ip))
            {
                throw new ConfigurationErrorsException($"Client with IP {ip} already added from {_ipClients[ip].Name}.config");
            }

            _ipClients.Add(ip, client);
            return this;
        }

        public IServiceConfigurationBuilder SetInvalidCredentialDelay(RandomWaiterConfig config)
        {
            InvalidCredentialDelay = config;
            return this;
        }

        public IServiceConfigurationBuilder SetServiceServerEndpoint(IPEndPoint endpoint)
        {
            ServiceServerEndpoint = endpoint;
            return this;
        }

        public IServiceConfigurationBuilder IsSingleClientMode(bool single)
        {
            SingleClientMode = single;
            return this;
        }

        public IServiceConfiguration Build()
        {
            return this;
        }
    }
}