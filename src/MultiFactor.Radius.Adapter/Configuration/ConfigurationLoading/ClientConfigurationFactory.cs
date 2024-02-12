//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

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
using Config = System.Configuration.Configuration;


namespace MultiFactor.Radius.Adapter.Configuration.ConfigurationLoading
{
    public class ClientConfigurationFactory
    {
        private readonly IRadiusDictionary _dictionary;

        public ClientConfigurationFactory(IRadiusDictionary dictionary)
        {
            _dictionary = dictionary;
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

            var builder = ClientConfiguration.CreateBuilder(name, radiusSharedSecretSetting, firstFactorAuthenticationSource,
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
           
            LoadAuthenticationSourceSettings(configuration, appSettings, builder);
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

            return builder.Build();
        }

        private static void LoadAuthenticationSourceSettings(Config configuration, AppSettingsSection appSettings, IClientConfigurationBuilder builder)
        {
            var ldapCatalogType = appSettings.Settings[Literals.Configuration.LdapCatalogType]?.Value;

            LdapCatalogType? ldapCatalogTypeParsed =  string.IsNullOrEmpty(ldapCatalogType) 
                ? null 
                : Enum.Parse<LdapCatalogType>(ldapCatalogType, true);
            
            switch (builder.Build().FirstFactorAuthenticationSource)
            {
                case AuthenticationSource.ActiveDirectory:
                    LoadActiveDirectoryAuthenticationSourceSettings(builder, appSettings, true);
                    if(ldapCatalogType != null && ldapCatalogTypeParsed != LdapCatalogType.ActiveDirectory)
                    {
                        throw new InvalidConfigurationException(
                            $"Invalid value for a {Literals.Configuration.LdapCatalogType} parameter." +
                            $" It should either be equal to ActiveDirectory or should be omitted, since {Literals.Configuration.FirstFactorAuthSource}" +
                            $" is {AuthenticationSource.ActiveDirectory}.");
                    }
                    builder.SetLdapCatalogType(ldapCatalogTypeParsed ?? LdapCatalogType.ActiveDirectory);
                    break;
                case AuthenticationSource.Ldap:
                    LoadActiveDirectoryAuthenticationSourceSettings(builder, appSettings, true);
                    builder.SetLdapCatalogType(ldapCatalogTypeParsed ?? LdapCatalogType.ActiveDirectory);
                    break;
                case AuthenticationSource.Radius:
                    LoadRadiusAuthenticationSourceSettings(builder, appSettings);
                    LoadActiveDirectoryAuthenticationSourceSettings(builder, appSettings, false);
                    // Use OpenLdap as default catalog since it is safier not use AD extensions
                    builder.SetLdapCatalogType(ldapCatalogTypeParsed ?? LdapCatalogType.OpenLdap);
                    break;
                case AuthenticationSource.None:
                    LoadActiveDirectoryAuthenticationSourceSettings(builder, appSettings, false);
                    builder.SetLdapCatalogType(ldapCatalogTypeParsed ?? LdapCatalogType.ActiveDirectory);
                    break;
            }
        }

        private static void LoadUserNameTransformRulesSection(Config configuration, IClientConfigurationBuilder builder)
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

        private static void LoadActiveDirectoryAuthenticationSourceSettings(IClientConfigurationBuilder builder, AppSettingsSection appSettings, bool mandatory)
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

            if (mandatory && string.IsNullOrEmpty(activeDirectoryDomainSetting))
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

            if (bool.TryParse(useUpnAsIdentitySetting, out var useUpnAsIdentity))
            {
                builder.SetUseUpnAsIdentity(useUpnAsIdentity);
            }
        }

        private static void LoadRadiusAuthenticationSourceSettings(IClientConfigurationBuilder builder, AppSettingsSection appSettings)
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

        private static void ReadSignUpGroupsSettings(IClientConfigurationBuilder builder, AppSettingsSection appSettings)
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

        private static void ReadAuthenticationCacheSettings(AppSettingsSection appSettings, IClientConfigurationBuilder builder)
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

        private static void LoadRadiusReplyAttributes(IClientConfigurationBuilder builder, IRadiusDictionary dictionary, RadiusReplyAttributesSection radiusReplyAttributesSection)
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