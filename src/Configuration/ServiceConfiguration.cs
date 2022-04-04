//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Core;
using MultiFactor.Radius.Adapter.Server;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Net;

namespace MultiFactor.Radius.Adapter.Configuration
{
    /// <summary>
    /// Service configuration
    /// </summary>
    public class ServiceConfiguration
    {
        private IDictionary<IPAddress, ClientConfiguration> _cients;

        public ServiceConfiguration()
        {
            _cients = new Dictionary<IPAddress, ClientConfiguration>();
        }

        private void AddClient(IPAddress ip, ClientConfiguration client)
        {
            if (_cients.ContainsKey(ip))
            {
                throw new ConfigurationErrorsException($"Client with IP {ip} already added");
            }
            _cients.Add(ip, client);
        }

        public ClientConfiguration GetClient(IPAddress ip)
        {
            if (SingleClientMode)
            {
                return _cients[IPAddress.Any];
            }
            if (_cients.ContainsKey(ip))
            {
                return _cients[ip];
            }
            return null;
        }

        #region common configuration settings

        /// <summary>
        /// This service RADIUS UDP Server endpoint
        /// </summary>
        public IPEndPoint ServiceServerEndpoint { get; set; }

        /// <summary>
        /// Multifactor API URL
        /// </summary>
        public string ApiUrl { get; set; }
        /// <summary>
        /// HTTP Proxy for API
        /// </summary>
        public string ApiProxy { get; set; }

        /// <summary>
        /// Logging level
        /// </summary>
        public string LogLevel { get; set; }

        public bool SingleClientMode { get; set; }

        #endregion

        public static string GetLogFormat()
        {
            var appSettings = ConfigurationManager.AppSettings;
            return appSettings?["logging-format"];
        }

        /// <summary>
        /// Read and load settings from appSettings configuration section
        /// </summary>
        public static ServiceConfiguration Load(IRadiusDictionary dictionary, ILogger logger)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            var serviceConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            var appSettingsSection = serviceConfig.GetSection("appSettings");
            var appSettings = appSettingsSection as AppSettingsSection;

            var serviceServerEndpointSetting    = appSettings.Settings["adapter-server-endpoint"]?.Value;
            var apiUrlSetting                   = appSettings.Settings["multifactor-api-url"]?.Value;
            var apiProxySetting                 = appSettings.Settings["multifactor-api-proxy"]?.Value;
            var logLevelSetting                 = appSettings.Settings["logging-level"]?.Value;

            if (string.IsNullOrEmpty(serviceServerEndpointSetting))
            {
                throw new Exception("Configuration error: 'adapter-server-endpoint' element not found");
            }
            if (string.IsNullOrEmpty(apiUrlSetting))
            {
                throw new Exception("Configuration error: 'multifactor-api-url' element not found");
            }
            if (string.IsNullOrEmpty(logLevelSetting))
            {
                throw new Exception("Configuration error: 'logging-level' element not found");
            }
            if (!TryParseIPEndPoint(serviceServerEndpointSetting, out var serviceServerEndpoint))
            {
                throw new Exception("Configuration error: Can't parse 'adapter-server-endpoint' value");
            }

            var configuration = new ServiceConfiguration
            {
                ServiceServerEndpoint = serviceServerEndpoint,
                ApiUrl = apiUrlSetting,
                ApiProxy = apiProxySetting,
                LogLevel = logLevelSetting
            };

            var clientConfigFilesPath = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) + Path.DirectorySeparatorChar + "clients";
            var clientConfigFiles = Directory.Exists(clientConfigFilesPath) ? Directory.GetFiles(clientConfigFilesPath, "*.config") : new string[0];

            if (clientConfigFiles.Length == 0)
            {
                var radiusReplyAttributesSection = ConfigurationManager.GetSection("RadiusReply") as RadiusReplyAttributesSection;

                var client = Load("General", dictionary, appSettings, radiusReplyAttributesSection, false);
                configuration.AddClient(IPAddress.Any, client);
                configuration.SingleClientMode = true;
            }
            else
            {
                foreach (var clientConfigFile in clientConfigFiles)
                {
                    logger.Information($"Loading client configuration from {Path.GetFileName(clientConfigFile)}");

                    var customConfigFileMap = new ExeConfigurationFileMap();
                    customConfigFileMap.ExeConfigFilename = clientConfigFile;

                    var config = ConfigurationManager.OpenMappedExeConfiguration(customConfigFileMap, ConfigurationUserLevel.None);
                    var clientSettings = (AppSettingsSection)config.GetSection("appSettings");
                    var radiusReplyAttributesSection = config.GetSection("RadiusReply") as RadiusReplyAttributesSection;

                    var client = Load(Path.GetFileNameWithoutExtension(clientConfigFile), dictionary, clientSettings, radiusReplyAttributesSection, true);

                    configuration.AddClient(client.Ip, client);
                }
            }

            return configuration;
        }

        public static ClientConfiguration Load(string name, IRadiusDictionary dictionary, AppSettingsSection appSettings, RadiusReplyAttributesSection radiusReplyAttributesSection, bool requiresClientIp)
        {       
            var radiusClientIpSetting                           = appSettings.Settings["radius-client-ip"]?.Value;
            var radiusSharedSecretSetting                       = appSettings.Settings["radius-shared-secret"]?.Value;
            var firstFactorAuthenticationSourceSettings         = appSettings.Settings["first-factor-authentication-source"]?.Value;
            var bypassSecondFactorWhenApiUnreachableSetting     = appSettings.Settings["bypass-second-factor-when-api-unreachable"]?.Value;
            var nasIdentifierSetting                            = appSettings.Settings["multifactor-nas-identifier"]?.Value;
            var multiFactorSharedSecretSetting                  = appSettings.Settings["multifactor-shared-secret"]?.Value;

            if (string.IsNullOrEmpty(firstFactorAuthenticationSourceSettings))
            {
                throw new Exception("Configuration error: 'first-factor-authentication-source' element not found");
            }

            if (string.IsNullOrEmpty(radiusSharedSecretSetting))
            {
                throw new Exception("Configuration error: 'radius-shared-secret' element not found");
            }

            if (string.IsNullOrEmpty(nasIdentifierSetting))
            {
                throw new Exception("Configuration error: 'multifactor-nas-identifier' element not found");
            }
            if (string.IsNullOrEmpty(multiFactorSharedSecretSetting))
            {
                throw new Exception("Configuration error: 'multifactor-shared-secret' element not found");
            }

            if (!Enum.TryParse<AuthenticationSource>(firstFactorAuthenticationSourceSettings, out var firstFactorAuthenticationSource))
            {
                throw new Exception("Configuration error: Can't parse 'first-factor-authentication-source' value. Must be one of: ActiveDirectory, Radius, None");
            }

            var configuration = new ClientConfiguration
            {
                Name = name,
                RadiusSharedSecret = radiusSharedSecretSetting,
                FirstFactorAuthenticationSource = firstFactorAuthenticationSource,
                NasIdentifier = nasIdentifierSetting,
                MultiFactorSharedSecret = multiFactorSharedSecretSetting,
            };

            if (requiresClientIp)
            {
                if (string.IsNullOrEmpty(radiusClientIpSetting))
                {
                    throw new Exception("Configuration error: 'radius-client-ip' element not found");
                }
                if (!IPAddress.TryParse(radiusClientIpSetting, out var clientIpAddress))
                {
                    throw new Exception("Configuration error: Can't parse 'radius-client-ip' value. Must be valid IPv4 address");
                }
                configuration.Ip = clientIpAddress;
            }

            if (bypassSecondFactorWhenApiUnreachableSetting != null)
            {
                if (bool.TryParse(bypassSecondFactorWhenApiUnreachableSetting, out var bypassSecondFactorWhenApiUnreachable))
                {
                    configuration.BypassSecondFactorWhenApiUnreachable = bypassSecondFactorWhenApiUnreachable;
                }
            }

            switch (configuration.FirstFactorAuthenticationSource)
            {
                case AuthenticationSource.ActiveDirectory:
                case AuthenticationSource.Ldap:
                    LoadActiveDirectoryAuthenticationSourceSettings(configuration, appSettings);
                    break;
                case AuthenticationSource.Radius:
                    LoadRadiusAuthenticationSourceSettings(configuration, appSettings);
                    break;
            }

            LoadRadiusReplyAttributes(configuration, dictionary, radiusReplyAttributesSection);

            return configuration;
        }

        private static void LoadActiveDirectoryAuthenticationSourceSettings(ClientConfiguration configuration, AppSettingsSection appSettings)
        {
            var activeDirectoryDomainSetting                        = appSettings.Settings["active-directory-domain"]?.Value;
            var ldapBindDnSetting                                   = appSettings.Settings["ldap-bind-dn"]?.Value;
            var activeDirectoryGroupSetting                         = appSettings.Settings["active-directory-group"]?.Value;
            var activeDirectory2FaGroupSetting                      = appSettings.Settings["active-directory-2fa-group"]?.Value;
            var useActiveDirectoryUserPhoneSetting                  = appSettings.Settings["use-active-directory-user-phone"]?.Value;
            var useActiveDirectoryMobileUserPhoneSetting            = appSettings.Settings["use-active-directory-mobile-user-phone"]?.Value;

            if (string.IsNullOrEmpty(activeDirectoryDomainSetting))
            {
                throw new Exception("Configuration error: 'active-directory-domain' element not found");
            }

            if (!string.IsNullOrEmpty(useActiveDirectoryUserPhoneSetting))
            {
                if (!bool.TryParse(useActiveDirectoryUserPhoneSetting, out var useActiveDirectoryUserPhone))
                {
                    throw new Exception("Configuration error: Can't parse 'use-active-directory-user-phone' value");
                }

                configuration.UseActiveDirectoryUserPhone = useActiveDirectoryUserPhone;
            }

            if (!string.IsNullOrEmpty(useActiveDirectoryMobileUserPhoneSetting))
            {
                if (!bool.TryParse(useActiveDirectoryMobileUserPhoneSetting, out var useActiveDirectoryMobileUserPhone))
                {
                    throw new Exception("Configuration error: Can't parse 'use-active-directory-mobile-user-phone' value");
                }

                configuration.UseActiveDirectoryMobileUserPhone = useActiveDirectoryMobileUserPhone;
            }

            configuration.ActiveDirectoryDomain = activeDirectoryDomainSetting;
            configuration.LdapBindDn = ldapBindDnSetting;
            configuration.ActiveDirectoryGroup = activeDirectoryGroupSetting;
            configuration.ActiveDirectory2FaGroup = activeDirectory2FaGroupSetting;
        }

        private static void LoadRadiusAuthenticationSourceSettings(ClientConfiguration configuration, AppSettingsSection appSettings)
        {
            var serviceClientEndpointSetting        = appSettings.Settings["adapter-client-endpoint"]?.Value;
            var npsEndpointSetting                  = appSettings.Settings["nps-server-endpoint"]?.Value;

            if (string.IsNullOrEmpty(serviceClientEndpointSetting))
            {
                throw new Exception("Configuration error: 'adapter-client-endpoint' element not found");
            }
            if (string.IsNullOrEmpty(npsEndpointSetting))
            {
                throw new Exception("Configuration error: 'nps-server-endpoint' element not found");
            }

            if (!TryParseIPEndPoint(serviceClientEndpointSetting, out var serviceClientEndpoint))
            {
                throw new Exception("Configuration error: Can't parse 'adapter-client-endpoint' value");
            }
            if (!TryParseIPEndPoint(npsEndpointSetting, out var npsEndpoint))
            {
                throw new Exception("Configuration error: Can't parse 'nps-server-endpoint' value");
            }

            configuration.ServiceClientEndpoint = serviceClientEndpoint;
            configuration.NpsServerEndpoint = npsEndpoint;
        }

        private static void LoadRadiusReplyAttributes(ClientConfiguration configuration, IRadiusDictionary dictionary, RadiusReplyAttributesSection radiusReplyAttributesSection)
        {
            var replyAttributes = new Dictionary<string, List<RadiusReplyAttributeValue>>();

            if (radiusReplyAttributesSection != null)
            {
                foreach (var member in radiusReplyAttributesSection.Members)
                {
                    var attribute = member as RadiusReplyAttributeElement;
                    var radiusAttribute = dictionary.GetAttribute(attribute.Name);
                    if (radiusAttribute == null)
                    {
                        throw new ConfigurationErrorsException($"Unknown attribute '{attribute.Name}' in RadiusReply configuration element, please see dictionary");
                    }
                    
                    if (!replyAttributes.ContainsKey(attribute.Name))
                    {
                        replyAttributes.Add(attribute.Name, new List<RadiusReplyAttributeValue>());
                    }

                    if (!string.IsNullOrEmpty(attribute.From))
                    {
                        replyAttributes[attribute.Name].Add(new RadiusReplyAttributeValue(attribute.From));
                    }
                    else
                    {
                        try
                        {
                            var value = ParseRadiusReplyAttributeValue(radiusAttribute, attribute.Value);
                            replyAttributes[attribute.Name].Add(new RadiusReplyAttributeValue(value, attribute.When));
                        }
                        catch (Exception ex)
                        {
                            throw new ConfigurationErrorsException($"Error while parsing attribute '{radiusAttribute.Name}' with {radiusAttribute.Type} value '{attribute.Value}' in RadiusReply configuration element: {ex.Message}");
                        }
                    }
                }
            }

            configuration.RadiusReplyAttributes = replyAttributes;
        }

        private static object ParseRadiusReplyAttributeValue(DictionaryAttribute attribute, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new Exception("Value must be specified");
            }
            
            switch (attribute.Type)
            {
                case DictionaryAttribute.TYPE_STRING:
                case DictionaryAttribute.TYPE_TAGGED_STRING:
                    return value;
                case DictionaryAttribute.TYPE_INTEGER:
                case DictionaryAttribute.TYPE_TAGGED_INTEGER:
                    return uint.Parse(value);
                case DictionaryAttribute.TYPE_IPADDR:
                    return IPAddress.Parse(value);
                case DictionaryAttribute.TYPE_OCTET:
                    return Utils.StringToByteArray(value);
                default:
                    throw new Exception($"Unknown type {attribute.Type}");
            }
        }

        private static bool TryParseIPEndPoint(string text, out IPEndPoint ipEndPoint)
        {
            Uri uri;
            ipEndPoint = null;

            if (Uri.TryCreate(string.Concat("tcp://", text), UriKind.Absolute, out uri))
            {
                ipEndPoint = new IPEndPoint(IPAddress.Parse(uri.Host), uri.Port < 0 ? 0 : uri.Port);
                return true;
            }
            if (Uri.TryCreate(string.Concat("tcp://", string.Concat("[", text, "]")), UriKind.Absolute, out uri))
            {
                ipEndPoint = new IPEndPoint(IPAddress.Parse(uri.Host), uri.Port < 0 ? 0 : uri.Port);
                return true;
            }

            throw new FormatException($"Failed to parse {text} to IPEndPoint");
        }
    }
}