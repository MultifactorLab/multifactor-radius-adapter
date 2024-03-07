//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Configuration.Features.AuthenticatedClientCacheFeature;
using MultiFactor.Radius.Adapter.Configuration.Features.PrivacyModeFeature;
using MultiFactor.Radius.Adapter.Configuration.Features.RadiusReplyAttributeFeature;
using MultiFactor.Radius.Adapter.Configuration.Features.UserNameTransformFeature;
using MultiFactor.Radius.Adapter.Core;
using MultiFactor.Radius.Adapter.Core.Exceptions;
using MultiFactor.Radius.Adapter.Core.Radius.Attributes;
using MultiFactor.Radius.Adapter.Server;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using static MultiFactor.Radius.Adapter.Core.Literals;
using Config = System.Configuration.Configuration;


namespace MultiFactor.Radius.Adapter.Configuration.ConfigurationLoading
{
    public class ClientConfigurationFactory
    {
        private readonly IRadiusDictionary _dictionary;
        private readonly ILogger<ClientConfigurationFactory> _logger;

        public ClientConfigurationFactory(IRadiusDictionary dictionary, ILogger<ClientConfigurationFactory> logger)
        {
            _dictionary = dictionary;
            _logger = logger;
        }

        public IClientConfiguration CreateConfig(string name, Config configuration)
        {
            var appSettings = configuration.AppSettings;
            var radiusSharedSecretSetting = appSettings.Settings[Literals.Configuration.RadiusSharedSecret]?.Value;
            var firstFactorAuthenticationSourceSettings = appSettings.Settings[Literals.Configuration.FirstFactorAuthSource]?.Value;
            var bypassSecondFactorWhenApiUnreachableSetting = appSettings.Settings[Literals.Configuration.BypassSecondFactorWhenApiUnreachable]?.Value;
            var multiFactorApiKeySetting = appSettings.Settings[Literals.Configuration.MultifactorNasIdentifier]?.Value;
            var multiFactorApiSecretSetting = appSettings.Settings[Literals.Configuration.MultifactorSharedSecret]?.Value;

            var serviceAccountUserSetting = appSettings.Settings[Literals.Configuration.ServiceAccountUser]?.Value;
            var serviceAccountPasswordSetting = appSettings.Settings[Literals.Configuration.ServiceAccountPassword]?.Value;

            if (string.IsNullOrEmpty(firstFactorAuthenticationSourceSettings))
            {
                throw new InvalidConfigurationException($"'{Literals.Configuration.FirstFactorAuthSource}' element not found");
            }

            if (string.IsNullOrEmpty(radiusSharedSecretSetting))
            {
                throw new InvalidConfigurationException($"'{Literals.Configuration.RadiusSharedSecret}' element not found");
            }

            if (string.IsNullOrEmpty(multiFactorApiKeySetting))
            {
                throw new InvalidConfigurationException($"'{Literals.Configuration.MultifactorNasIdentifier}' element not found");
            }
            if (string.IsNullOrEmpty(multiFactorApiSecretSetting))
            {
                throw new InvalidConfigurationException($"'{Literals.Configuration.MultifactorSharedSecret}' element not found");
            }

            var isDigit = int.TryParse(firstFactorAuthenticationSourceSettings, out _);
            if (isDigit || !Enum.TryParse<AuthenticationSource>(firstFactorAuthenticationSourceSettings, true, out var firstFactorAuthenticationSource))
            {
                throw new InvalidConfigurationException($"Can't parse '{Literals.Configuration.FirstFactorAuthSource}' value. Must be one of: ActiveDirectory, Radius, None");
            }

            var builder = new ClientConfiguration(name, radiusSharedSecretSetting, firstFactorAuthenticationSource,
                multiFactorApiKeySetting, multiFactorApiSecretSetting);

            if (bypassSecondFactorWhenApiUnreachableSetting != null)
            {
                if (bool.TryParse(bypassSecondFactorWhenApiUnreachableSetting, out var bypassSecondFactorWhenApiUnreachable))
                {
                    builder.SetBypassSecondFactorWhenApiUnreachable(bypassSecondFactorWhenApiUnreachable);
                }
            }

            try
            {
                builder.SetPrivacyMode(PrivacyModeDescriptor.Create(appSettings.Settings[Literals.Configuration.PrivacyMode]?.Value));
            }
            catch
            {
                throw new InvalidConfigurationException($"Can't parse '{Literals.Configuration.PrivacyMode}' value. Must be one of: Full, None, Partial:Field1,Field2");
            }

            switch (builder.FirstFactorAuthenticationSource)
            {
                case AuthenticationSource.ActiveDirectory:
                case AuthenticationSource.Ldap:
                    LoadActiveDirectoryAuthenticationSourceSettings(builder, appSettings);
                    break;
                case AuthenticationSource.Radius:
                    LoadRadiusAuthenticationSourceSettings(builder, appSettings);
                    LoadActiveDirectoryAuthenticationSourceSettings(builder, appSettings);
                    break;
                case AuthenticationSource.None:
                    LoadActiveDirectoryAuthenticationSourceSettings(builder, appSettings);
                    break;
            }

            if (builder.CheckMembership && string.IsNullOrEmpty(builder.ActiveDirectoryDomain))
            {
                throw new InvalidConfigurationException("membership verification impossible: 'active-directory-domain' element not found");
            }

            LoadRadiusReplyAttributes(builder, _dictionary, configuration.GetSection("RadiusReply") as RadiusReplyAttributesSection);
            LoadUserNameTransformRulesSection(configuration, builder);

            builder.SetServiceAccountUser(serviceAccountUserSetting ?? string.Empty);
            builder.SetServiceAccountPassword(serviceAccountPasswordSetting ?? string.Empty);

            ReadSignUpGroupsSettings(builder, appSettings);
            ReadAuthenticationCacheSettings(appSettings, builder);

            var callindStationIdAttr = appSettings.Settings[Literals.Configuration.CallingStationIdAttribute]?.Value;
            if (!string.IsNullOrWhiteSpace(callindStationIdAttr))
            {
                builder.SetCallingStationIdVendorAttribute(callindStationIdAttr);
            }

            return builder;
        }

        private static void LoadUserNameTransformRulesSection(Config configuration, ClientConfiguration builder)
        {
            var userNameTransformRulesSection = configuration.GetSection("UserNameTransformRules") as UserNameTransformRulesSection;

            if (userNameTransformRulesSection?.Members != null)
            {
                foreach (var member in userNameTransformRulesSection?.Members)
                {
                    if (member is UserNameTransformRulesElement rule)
                    {
                        builder.AddUserNameTransformRule(rule);
                    }
                }
            }
        }

        private void LoadActiveDirectoryAuthenticationSourceSettings(ClientConfiguration builder, AppSettingsSection appSettings)
        {
            var activeDirectoryDomainSetting = appSettings.Settings["active-directory-domain"]?.Value;
            var ldapBindDnSetting = appSettings.Settings["ldap-bind-dn"]?.Value;
            var activeDirectoryGroupSetting = appSettings.Settings["active-directory-group"]?.Value;
            var activeDirectory2FaGroupSetting = appSettings.Settings["active-directory-2fa-group"]?.Value;
            var activeDirectory2FaBypassGroupSetting = appSettings.Settings["active-directory-2fa-bypass-group"]?.Value;
            var useActiveDirectoryUserPhoneSetting = appSettings.Settings["use-active-directory-user-phone"]?.Value;
            var useActiveDirectoryMobileUserPhoneSetting = appSettings.Settings["use-active-directory-mobile-user-phone"]?.Value;
            var phoneAttributes = appSettings.Settings["phone-attribute"]?.Value;
            var loadActiveDirectoryNestedGroupsSettings = appSettings.Settings["load-active-directory-nested-groups"]?.Value;
            var useUpnAsIdentitySetting = appSettings.Settings["use-upn-as-identity"]?.Value;
            var twoFAIdentityAttribyteSetting = appSettings.Settings["use-attribute-as-identity"]?.Value;

            if (builder.FirstFactorAuthenticationSource == AuthenticationSource.ActiveDirectory && string.IsNullOrEmpty(activeDirectoryDomainSetting))
            {
                throw new InvalidConfigurationException("'active-directory-domain' element not found");
            }

            //legacy settings for general phone attribute usage
            if (bool.TryParse(useActiveDirectoryUserPhoneSetting, out var useActiveDirectoryUserPhone))
            {
                if (useActiveDirectoryUserPhone)
                {
                    builder.AddPhoneAttribute("telephoneNumber");
                }
            }

            //legacy settings for mobile phone attribute usage
            if (bool.TryParse(useActiveDirectoryMobileUserPhoneSetting, out var useActiveDirectoryMobileUserPhone))
            {
                if (useActiveDirectoryMobileUserPhone)
                {
                    builder.AddPhoneAttribute("mobile");
                }
            }

            if (!string.IsNullOrEmpty(phoneAttributes))
            {
                var attrs = phoneAttributes.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(attr => attr.Trim()).ToList();
                builder.AddPhoneAttributes(attrs);
            }

            if (!string.IsNullOrEmpty(loadActiveDirectoryNestedGroupsSettings))
            {
                if (!bool.TryParse(loadActiveDirectoryNestedGroupsSettings, out var loadActiveDirectoryNestedGroups))
                {
                    throw new InvalidConfigurationException("Can't parse 'load-active-directory-nested-groups' value");
                }

                builder.SetLoadActiveDirectoryNestedGroups(loadActiveDirectoryNestedGroups);
            }

            if (!string.IsNullOrWhiteSpace(activeDirectoryDomainSetting))
            {
                builder.SetActiveDirectoryDomain(activeDirectoryDomainSetting);
            }

            if (!string.IsNullOrWhiteSpace(ldapBindDnSetting))
            {
                builder.SetLdapBindDn(ldapBindDnSetting);
            }

            if (!string.IsNullOrEmpty(activeDirectoryGroupSetting))
            {
                builder.AddActiveDirectoryGroups(activeDirectoryGroupSetting.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            }

            if (!string.IsNullOrEmpty(activeDirectory2FaGroupSetting))
            {
                builder.AddActiveDirectory2FaGroups(activeDirectory2FaGroupSetting.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            }

            if (!string.IsNullOrEmpty(activeDirectory2FaBypassGroupSetting))
            {
                builder.AddActiveDirectory2FaBypassGroups(activeDirectory2FaBypassGroupSetting.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            }

            // MUST be before 'use-upn-as-identity' check
            if (!string.IsNullOrEmpty(twoFAIdentityAttribyteSetting))
            {
                builder.SetUseAttributeAsIdentity(twoFAIdentityAttribyteSetting);
            }

            if (bool.TryParse(useUpnAsIdentitySetting, out var useUpnAsIdentity))
            {
                if (!string.IsNullOrEmpty(twoFAIdentityAttribyteSetting))
                    throw new Exception("Configuration error: Using settings 'use-upn-as-identity' and 'use-attribute-as-identity' together is unacceptable. Prefer using 'use-attribute-as-identity'.");

                _logger.LogWarning("The setting 'use-upn-as-identity' is deprecated, use 'use-attribute-as-identity' instead");
                builder.SetUseAttributeAsIdentity("userPrincipalName");
            }
        }

        private static void LoadRadiusAuthenticationSourceSettings(ClientConfiguration builder, AppSettingsSection appSettings)
        {
            var serviceClientEndpointSetting = appSettings.Settings["adapter-client-endpoint"]?.Value;
            var npsEndpointSetting = appSettings.Settings["nps-server-endpoint"]?.Value;

            if (string.IsNullOrEmpty(serviceClientEndpointSetting))
            {
                throw new InvalidConfigurationException("'adapter-client-endpoint' element not found");
            }
            if (string.IsNullOrEmpty(npsEndpointSetting))
            {
                throw new InvalidConfigurationException("'nps-server-endpoint' element not found");
            }

            if (!IPEndPointFactory.TryParse(serviceClientEndpointSetting, out var serviceClientEndpoint))
            {
                throw new InvalidConfigurationException("Can't parse 'adapter-client-endpoint' value");
            }
            if (!IPEndPointFactory.TryParse(npsEndpointSetting, out var npsEndpoint))
            {
                throw new InvalidConfigurationException("Can't parse 'nps-server-endpoint' value");
            }

            builder.SetServiceClientEndpoint(serviceClientEndpoint);
            builder.SetNpsServerEndpoint(npsEndpoint);
        }

        private static void ReadSignUpGroupsSettings(ClientConfiguration builder, AppSettingsSection appSettings)
        {
            const string signUpGroupsRegex = @"([\wа-я\s\-]+)(\s*;\s*([\wа-я\s\-]+)*)*";
            const string signUpGroupsToken = "sign-up-groups";

            var signUpGroupsSettings = appSettings.Settings[signUpGroupsToken]?.Value;
            if (string.IsNullOrWhiteSpace(signUpGroupsSettings))
            {
                builder.SetSignUpGroups(string.Empty);
                return;
            }

            if (!Regex.IsMatch(signUpGroupsSettings, signUpGroupsRegex, RegexOptions.IgnoreCase))
            {
                throw new InvalidConfigurationException($"Invalid group names. Please check 'sign-up-groups' settings property and fix syntax errors.");
            }

            builder.SetSignUpGroups(signUpGroupsSettings);
        }

        private static void ReadAuthenticationCacheSettings(AppSettingsSection appSettings, ClientConfiguration builder)
        {
            bool minimalMatching = false;
            try
            {
                minimalMatching = bool.Parse(appSettings.Settings[Literals.Configuration.AuthenticationCacheMinimalMatching]?.Value ?? bool.FalseString);
            }
            catch
            {
                throw new InvalidConfigurationException($"Can't parse '{Literals.Configuration.AuthenticationCacheMinimalMatching}' value");
            }

            try
            {
                var ltConf = AuthenticatedClientCacheConfig.Create(appSettings.Settings[Literals.Configuration.AuthenticationCacheLifetime]?.Value, minimalMatching);
                builder.SetAuthenticationCacheLifetime(ltConf);
            }
            catch
            {
                throw new InvalidConfigurationException($"Can't parse '{appSettings.Settings[Literals.Configuration.AuthenticationCacheLifetime]?.Value}' value");
            }
        }

        private static void LoadRadiusReplyAttributes(ClientConfiguration builder, IRadiusDictionary dictionary, RadiusReplyAttributesSection radiusReplyAttributesSection)
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
                        throw new InvalidConfigurationException($"Unknown attribute '{attribute.Name}' in RadiusReply configuration element, please see dictionary");
                    }

                    if (!replyAttributes.ContainsKey(attribute.Name))
                    {
                        replyAttributes.Add(attribute.Name, new List<RadiusReplyAttributeValue>());
                    }

                    if (!string.IsNullOrEmpty(attribute.From))
                    {
                        replyAttributes[attribute.Name].Add(new RadiusReplyAttributeValue(attribute.From, attribute.Sufficient));
                    }
                    else
                    {
                        try
                        {
                            var value = ParseRadiusReplyAttributeValue(radiusAttribute, attribute.Value);
                            replyAttributes[attribute.Name].Add(new RadiusReplyAttributeValue(value, attribute.When, attribute.Sufficient));
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidConfigurationException($"Error while parsing attribute '{radiusAttribute.Name}' with {radiusAttribute.Type} value '{attribute.Value}' in RadiusReply configuration element: {ex.Message}");
                        }
                    }
                }
            }

            foreach (var attr in replyAttributes)
            {
                builder.AddRadiusReplyAttribute(attr.Key, attr.Value);

            }
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

    }
}