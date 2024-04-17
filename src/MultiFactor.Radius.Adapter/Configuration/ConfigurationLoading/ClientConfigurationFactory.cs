//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Configuration.Features.AuthenticatedClientCacheFeature;
using MultiFactor.Radius.Adapter.Configuration.Features.PreAuthModeFeature;
using MultiFactor.Radius.Adapter.Configuration.Features.PrivacyModeFeature;
using MultiFactor.Radius.Adapter.Configuration.Features.RadiusReplyAttributeFeature;
using MultiFactor.Radius.Adapter.Configuration.Features.RandomWaiterFeature;
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

namespace MultiFactor.Radius.Adapter.Configuration.ConfigurationLoading;

public class ClientConfigurationFactory
{
    private readonly IRadiusDictionary _dictionary;
    private readonly ILogger<ClientConfigurationFactory> _logger;

    public ClientConfigurationFactory(IRadiusDictionary dictionary, ILogger<ClientConfigurationFactory> logger)
    {
        _dictionary = dictionary;
        _logger = logger;
    }

    public IClientConfiguration CreateConfig(string name, Config configuration, IServiceConfiguration serviceConfig)
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

        ReadPrivacyModeSetting(appSettings, builder);
        ReadInvalidCredDelaySetting(appSettings, builder, serviceConfig);
        ReadPreAuthModeSetting(appSettings, builder);

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
            throw new InvalidConfigurationException($"Membership verification impossible: '{Literals.Configuration.ActiveDirectoryDomain}' element not found");
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

    private static void ReadInvalidCredDelaySetting(AppSettingsSection appSettings, ClientConfiguration builder, IServiceConfiguration serviceConfig)
    {
        var credDelay = appSettings.Settings[Literals.Configuration.InvalidCredentialDelay]?.Value;
        if (string.IsNullOrWhiteSpace(credDelay))
        {
            builder.SetInvalidCredentialDelay(serviceConfig.InvalidCredentialDelay);
            return;
        }

        try
        {
            var waiterConfig = RandomWaiterConfig.Create(credDelay);
            builder.SetInvalidCredentialDelay(waiterConfig);
        }
        catch
        {
            throw new InvalidConfigurationException($"Can't parse '{Literals.Configuration.InvalidCredentialDelay}' value");
        }
    }

    private static void ReadPreAuthModeSetting(AppSettingsSection appSettings, ClientConfiguration builder)
    {
        try
        {
            builder.SetPreAuthMode(PreAuthModeDescriptor.Create(appSettings.Settings[Literals.Configuration.PreAuthMode]?.Value, PreAuthModeSettings.Default));
        }
        catch
        {
            throw new InvalidConfigurationException($"Can't parse '{Literals.Configuration.PreAuthMode}' value. Must be one of: {PreAuthModeDescriptor.DisplayAvailableModes()}");
        }

        if (builder.PreAuthnMode.Mode != PreAuthMode.None && builder.InvalidCredentialDelay.Min < 2)
        {
            throw new InvalidConfigurationException($"To enable pre-auth second factor for this client please set '{Literals.Configuration.InvalidCredentialDelay}' min value to 2 or more");
        }
    }

    private static void ReadPrivacyModeSetting(AppSettingsSection appSettings, ClientConfiguration builder)
    {       
        try
        {
            builder.SetPrivacyMode(PrivacyModeDescriptor.Create(appSettings.Settings[Literals.Configuration.PrivacyMode]?.Value));
        }
        catch
        {
            throw new InvalidConfigurationException($"Can't parse '{Literals.Configuration.PrivacyMode}' value. Must be one of: Full, None, Partial:Field1,Field2");
        }     
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
        var settings = appSettings.Settings;
        var activeDirectoryDomainSetting = settings[Literals.Configuration.ActiveDirectoryDomain]?.Value;
        var ldapBindDnSetting = settings[Literals.Configuration.LdapBindDn]?.Value;
        var activeDirectoryGroupSetting = settings[Literals.Configuration.ActiveDirectoryGroup]?.Value;
        var activeDirectory2FaGroupSetting = settings[Literals.Configuration.ActiveDirectory2FaGroup]?.Value;
        var activeDirectory2FaBypassGroupSetting = settings[Literals.Configuration.ActiveDirectory2FaBypassGroup]?.Value;
        var useActiveDirectoryUserPhoneSetting = settings[Literals.Configuration.UseActiveDirectoryUserPhone]?.Value;
        var useActiveDirectoryMobileUserPhoneSetting = settings[Literals.Configuration.UseActiveDirectoryMobileUserPhone]?.Value;
        var phoneAttributes = settings[Literals.Configuration.PhoneAttribute]?.Value;
        var loadActiveDirectoryNestedGroupsSettings = settings[Literals.Configuration.LoadActiveDirectoryNestedGroups]?.Value;
        var useUpnAsIdentitySetting = settings[Literals.Configuration.UseUpnAsIdentity]?.Value;
        var twoFAIdentityAttribyteSetting = settings[Literals.Configuration.UseAttributeAsIdentity]?.Value;

        if (builder.FirstFactorAuthenticationSource == AuthenticationSource.ActiveDirectory && string.IsNullOrEmpty(activeDirectoryDomainSetting))
        {
            throw new InvalidConfigurationException($"'{Literals.Configuration.ActiveDirectoryDomain}' element not found");
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
                throw new InvalidConfigurationException($"Can't parse '{Literals.Configuration.LoadActiveDirectoryNestedGroups}' value");
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
                throw new InvalidConfigurationException($"Using settings '{Literals.Configuration.UseUpnAsIdentity}' and '{Literals.Configuration.UseAttributeAsIdentity}' together is unacceptable. Prefer using '{Literals.Configuration.UseAttributeAsIdentity}'.");

            _logger.LogWarning($"The setting '{Literals.Configuration.UseUpnAsIdentity}' is deprecated, use '{Literals.Configuration.UseAttributeAsIdentity}' instead");
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

        var signUpGroupsSettings = appSettings.Settings[Literals.Configuration.SignUpGroups]?.Value;
        if (string.IsNullOrWhiteSpace(signUpGroupsSettings))
        {
            builder.SetSignUpGroups(string.Empty);
            return;
        }

        if (!Regex.IsMatch(signUpGroupsSettings, signUpGroupsRegex, RegexOptions.IgnoreCase))
        {
            throw new InvalidConfigurationException($"Invalid group names. Please check '{Literals.Configuration.SignUpGroups}' settings property and fix syntax errors.");
        }

        builder.SetSignUpGroups(signUpGroupsSettings);
    }

    private static void ReadAuthenticationCacheSettings(AppSettingsSection appSettings, ClientConfiguration builder)
    {
        bool minimalMatching;
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
                var radiusAttribute = dictionary.GetAttribute(attribute.Name)
                    ?? throw new InvalidConfigurationException($"Unknown attribute '{attribute.Name}' in RadiusReply configuration element, please see dictionary");
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

        return attribute.Type switch
        {
            DictionaryAttribute.TYPE_STRING or DictionaryAttribute.TYPE_TAGGED_STRING => value,
            DictionaryAttribute.TYPE_INTEGER or DictionaryAttribute.TYPE_TAGGED_INTEGER => uint.Parse(value),
            DictionaryAttribute.TYPE_IPADDR => IPAddress.Parse(value),
            DictionaryAttribute.TYPE_OCTET => Utils.StringToByteArray(value),
            _ => throw new Exception($"Unknown type {attribute.Type}"),
        };
    }
}
