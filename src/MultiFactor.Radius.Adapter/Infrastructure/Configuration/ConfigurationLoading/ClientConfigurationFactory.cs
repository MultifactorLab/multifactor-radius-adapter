//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Core.Radius.Attributes;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Exceptions;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Features.AuthenticatedClientCacheFeature;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Features.PreAuthModeFeature;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Features.PrivacyModeFeature;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Features.RandomWaiterFeature;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models.RadiusReply;
using MultiFactor.Radius.Adapter.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.ClientLevel;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.RootLevel;

namespace MultiFactor.Radius.Adapter.Infrastructure.Configuration.ConfigurationLoading;

public class ClientConfigurationFactory
{
    private readonly IRadiusDictionary _dictionary;
    private readonly ILogger<ClientConfigurationFactory> _logger;

    public ClientConfigurationFactory(IRadiusDictionary dictionary, ILogger<ClientConfigurationFactory> logger)
    {
        _dictionary = dictionary;
        _logger = logger;
    }

    public IClientConfiguration CreateConfig(string name, RadiusAdapterConfiguration configuration, IServiceConfiguration serviceConfig)
    {
        var appSettings = configuration.AppSettings;

        if (string.IsNullOrEmpty(appSettings.FirstFactorAuthenticationSource))
        {
            throw InvalidConfigurationException.For(x => x.AppSettings.FirstFactorAuthenticationSource, 
                "'{prop}' element not found. Config name: '{0}'",
                name);
        }

        var isDigit = int.TryParse(appSettings.FirstFactorAuthenticationSource, out _);
        if (isDigit || !Enum.TryParse<AuthenticationSource>(appSettings.FirstFactorAuthenticationSource, true, out var firstFactorAuthenticationSource))
        {
            throw InvalidConfigurationException.For(x => x.AppSettings.FirstFactorAuthenticationSource,
                "Can't parse '{prop}' value. Must be one of: ActiveDirectory, Radius, None. Config name: '{0}'", name);
        }

        if (string.IsNullOrEmpty(appSettings.RadiusSharedSecret))
        {
            throw InvalidConfigurationException.For(x => x.AppSettings.RadiusSharedSecret,
                "'{prop}' element not found. Config name: '{0}'",
                name);
        }

        if (string.IsNullOrEmpty(appSettings.MultifactorNasIdentifier))
        {
            throw InvalidConfigurationException.For(x => x.AppSettings.MultifactorNasIdentifier,
                "'{prop}' element not found. Config name: '{0}'",
                name);
        }

        if (string.IsNullOrEmpty(appSettings.MultifactorSharedSecret))
        {
            throw InvalidConfigurationException.For(x => x.AppSettings.MultifactorSharedSecret,
                "'{prop}' element not found. Config name: '{0}'",
                name);
        }

        var builder = new ClientConfiguration(name,
            appSettings.RadiusSharedSecret, 
            firstFactorAuthenticationSource,
            appSettings.MultifactorNasIdentifier,
            appSettings.MultifactorSharedSecret);

        builder.SetBypassSecondFactorWhenApiUnreachable(appSettings.BypassSecondFactorWhenApiUnreachable);

        ReadPrivacyModeSetting(appSettings, builder);
        ReadInvalidCredDelaySetting(appSettings, builder, serviceConfig);
        ReadPreAuthModeSetting(appSettings, builder);

        switch (builder.FirstFactorAuthenticationSource)
        {
            case AuthenticationSource.ActiveDirectory:
            case AuthenticationSource.Ldap:
                ReadActiveDirectoryAuthenticationSourceSettings(builder, appSettings);
                break;
            case AuthenticationSource.Radius:
                ReadRadiusAuthenticationSourceSettings(builder, appSettings);
                ReadActiveDirectoryAuthenticationSourceSettings(builder, appSettings);
                break;
            case AuthenticationSource.None:
                ReadActiveDirectoryAuthenticationSourceSettings(builder, appSettings);
                break;
        }

        if (builder.CheckMembership && string.IsNullOrEmpty(builder.ActiveDirectoryDomain))
        {
            throw InvalidConfigurationException.For(x => x.AppSettings.ActiveDirectoryDomain,
                "Membership verification impossible: '{prop}' element not found. Config name: '{0}'",
                builder.Name);
        }

        ReadRadiusReplyAttributes(builder, _dictionary, configuration.RadiusReply);
        ReadUserNameTransformRulesSection(configuration, builder);

        builder.SetServiceAccountUser(appSettings.ServiceAccountUser ?? string.Empty);
        builder.SetServiceAccountPassword(appSettings.ServiceAccountPassword ?? string.Empty);

        ReadSignUpGroupsSettings(builder, appSettings);
        ReadAuthenticationCacheSettings(appSettings, builder);

        var callindStationIdAttr = appSettings.CallingStationIdAttribute;
        if (!string.IsNullOrWhiteSpace(callindStationIdAttr))
        {
            builder.SetCallingStationIdVendorAttribute(callindStationIdAttr);
        }

        return builder;
    }

    private static void ReadInvalidCredDelaySetting(AppSettingsSection appSettings, ClientConfiguration builder, IServiceConfiguration serviceConfig)
    {
        var credDelay = appSettings.InvalidCredentialDelay;
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
            throw InvalidConfigurationException.For(x => x.AppSettings.InvalidCredentialDelay,
                "Can't parse '{prop}' value. Config name: '{0}'",
                builder.Name);
        }
    }

    private static void ReadPreAuthModeSetting(AppSettingsSection appSettings, ClientConfiguration builder)
    {
        try
        {
            builder.SetPreAuthMode(PreAuthModeDescriptor.Create(appSettings.PreAuthenticationMethod, PreAuthModeSettings.Default));
        }
        catch
        {
            throw InvalidConfigurationException.For(x => x.AppSettings.PreAuthenticationMethod,
                "Can't parse '{prop}' value. Must be one of: {0}. Config name: '{1}'", PreAuthModeDescriptor.DisplayAvailableModes(),
                builder.Name);
        }

        if (builder.PreAuthnMode.Mode != PreAuthMode.None && builder.InvalidCredentialDelay.Min < 2)
        {
            throw InvalidConfigurationException.For(x => x.AppSettings.InvalidCredentialDelay,
                "To enable pre-auth second factor for this client please set '{prop}' min value to 2 or more. Config name: '{0}'",
                builder.Name);
        }
    }

    private static void ReadPrivacyModeSetting(AppSettingsSection appSettings, ClientConfiguration builder)
    {
        try
        {
            builder.SetPrivacyMode(PrivacyModeDescriptor.Create(appSettings.PrivacyMode));
        }
        catch
        {
            throw InvalidConfigurationException.For(x => x.AppSettings.PrivacyMode,
                "Can't parse '{prop}' value. Must be one of: Full, None, Partial:Field1,Field2. Config name: '{0}'",
                builder.Name);
        }
    }

    private static void ReadUserNameTransformRulesSection(RadiusAdapterConfiguration configuration, ClientConfiguration builder)
    { 
        foreach (var rule in configuration.UserNameTransformRules.Elements)
        {
            builder.AddUserNameTransformRule(rule);
        }  
    }

    private void ReadActiveDirectoryAuthenticationSourceSettings(ClientConfiguration builder, AppSettingsSection appSettings)
    {
        if (builder.FirstFactorAuthenticationSource == AuthenticationSource.ActiveDirectory && string.IsNullOrEmpty(appSettings.ActiveDirectoryDomain))
        {
            throw InvalidConfigurationException.For(x => x.AppSettings.ActiveDirectoryDomain,
                "'{prop}' element not found. Config name: '{0}'",
                builder.Name);
        }

        //legacy settings for general phone attribute usage      
        if (appSettings.UseActiveDirectoryUserPhone)
        {
            builder.AddPhoneAttribute("telephoneNumber");
        }

        //legacy settings for mobile phone attribute usage
        if (appSettings.UseActiveDirectoryMobileUserPhone)
        {
            builder.AddPhoneAttribute("mobile");
        }

        if (!string.IsNullOrEmpty(appSettings.PhoneAttribute))
        {
            var attrs = appSettings.PhoneAttribute.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(attr => attr.Trim()).ToList();
            builder.AddPhoneAttributes(attrs);
        }

        builder.SetLoadActiveDirectoryNestedGroups(appSettings.LoadActiveDirectoryNestedGroups);

        if (!string.IsNullOrWhiteSpace(appSettings.ActiveDirectoryDomain))
        {
            builder.SetActiveDirectoryDomain(appSettings.ActiveDirectoryDomain);
        }

        if (!string.IsNullOrWhiteSpace(appSettings.LdapBindDn))
        {
            builder.SetLdapBindDn(appSettings.LdapBindDn);
        }

        if (!string.IsNullOrEmpty(appSettings.ActiveDirectoryGroup))
        {
            builder.AddActiveDirectoryGroups(appSettings.ActiveDirectoryGroup.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
        }

        if (!string.IsNullOrEmpty(appSettings.ActiveDirectory2faGroup))
        {
            builder.AddActiveDirectory2FaGroups(appSettings.ActiveDirectory2faGroup.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
        }

        if (!string.IsNullOrEmpty(appSettings.ActiveDirectory2faBypassGroup))
        {
            builder.AddActiveDirectory2FaBypassGroups(appSettings.ActiveDirectory2faBypassGroup.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
        }

        ReadCustomIdentitySettings(builder, appSettings);
    }

    private void ReadCustomIdentitySettings(ClientConfiguration builder, AppSettingsSection appSettings)
    {
        // MUST be before 'use-upn-as-identity' check
        var hasUseAttributeAsIdentity = !string.IsNullOrEmpty(appSettings.UseAttributeAsIdentity);
        var hasUseUpnAsIdentity = !string.IsNullOrEmpty(appSettings.UseUpnAsIdentity);
        if (hasUseUpnAsIdentity && hasUseAttributeAsIdentity)
        {
            throw InvalidConfigurationException.For(x => x.AppSettings.UseUpnAsIdentity,
                "Using settings '{prop}' and '{0}' together is unacceptable. Prefer using '{0}'. Config name: '{1}'",
                RadiusAdapterConfigurationDescription.Property(x => x.AppSettings.UseAttributeAsIdentity),
                builder.Name);
        }

        if (hasUseAttributeAsIdentity)
        {
            builder.SetUseAttributeAsIdentity(appSettings.UseAttributeAsIdentity);
        }

        if (hasUseUpnAsIdentity)
        {
            if (!bool.TryParse(appSettings.UseUpnAsIdentity, out var parsed))
            {
                throw InvalidConfigurationException.For(x => x.AppSettings.UseUpnAsIdentity,
                    "Can't parse '{prop}' value. Config name: '{0}'",
                    builder.Name);
            }

            if (parsed)
            {
                _logger.LogWarning("The setting '{UseUpnAsIdentity:l}' is deprecated, use '{UseAttributeAsIdentity:l}' instead",
                    RadiusAdapterConfigurationDescription.Property(x => x.AppSettings.UseUpnAsIdentity),
                    RadiusAdapterConfigurationDescription.Property(x => x.AppSettings.UseAttributeAsIdentity));

                builder.SetUseAttributeAsIdentity("userPrincipalName");
            }
        }
    }

    private static void ReadRadiusAuthenticationSourceSettings(ClientConfiguration builder, AppSettingsSection appSettings)
    {
        if (string.IsNullOrEmpty(appSettings.AdapterClientEndpoint))
        {
            throw InvalidConfigurationException.For(x => x.AppSettings.AdapterClientEndpoint, 
                "'{prop}' element not found. Config name: '{0}'",
                builder.Name);
        }
        if (string.IsNullOrEmpty(appSettings.NpsServerEndpoint))
        {
            throw InvalidConfigurationException.For(x => x.AppSettings.NpsServerEndpoint,
                "'{prop}' element not found. Config name: '{0}'",
                builder.Name);
        }

        if (!IPEndPointFactory.TryParse(appSettings.AdapterClientEndpoint, out var serviceClientEndpoint))
        {
            throw InvalidConfigurationException.For(x => x.AppSettings.AdapterClientEndpoint,
                "Can't parse '{prop}' value. Config name: '{0}'",
                builder.Name);
        }
        if (!IPEndPointFactory.TryParse(appSettings.NpsServerEndpoint, out var npsEndpoint))
        {
            throw InvalidConfigurationException.For(x => x.AppSettings.NpsServerEndpoint,
                "Can't parse '{prop}' value. Config name: '{0}'",
                builder.Name);
        }

        builder.SetServiceClientEndpoint(serviceClientEndpoint);
        builder.SetNpsServerEndpoint(npsEndpoint);
    }

    private static void ReadSignUpGroupsSettings(ClientConfiguration builder, AppSettingsSection appSettings)
    {
        const string signUpGroupsRegex = @"([\wа-я\s\-]+)(\s*;\s*([\wа-я\s\-]+)*)*";

        var signUpGroupsSettings = appSettings.SignUpGroups;
        if (string.IsNullOrWhiteSpace(signUpGroupsSettings))
        {
            builder.SetSignUpGroups(string.Empty);
            return;
        }

        if (!Regex.IsMatch(signUpGroupsSettings, signUpGroupsRegex, RegexOptions.IgnoreCase))
        {
            throw InvalidConfigurationException.For(x => x.AppSettings.SignUpGroups,
                "Invalid group names. Please check '{prop}' settings property and fix syntax errors. Config name: '{0}'",
                builder.Name);
        }

        builder.SetSignUpGroups(signUpGroupsSettings);
    }

    private static void ReadAuthenticationCacheSettings(AppSettingsSection appSettings, ClientConfiguration builder)
    {
        try
        {
            var ltConf = AuthenticatedClientCacheConfig.Create(appSettings.AuthenticationCacheLifetime, appSettings.AuthenticationCacheMinimalMatching);
            builder.SetAuthenticationCacheLifetime(ltConf);
        }
        catch
        {
            throw InvalidConfigurationException.For(x => x.AppSettings.AuthenticationCacheLifetime,
                "Can't parse '{prop}' value. Config name: '{0}'",
                builder.Name);
        }
    }

    private static void ReadRadiusReplyAttributes(ClientConfiguration builder, IRadiusDictionary dictionary, RadiusReplySection radiusReplyAttributesSection)
    {
        var replyAttributes = new Dictionary<string, List<RadiusReplyAttributeValue>>();

        if (radiusReplyAttributesSection != null)
        {
            foreach (var attribute in radiusReplyAttributesSection.Attributes.Elements)
            {
                var radiusAttribute = dictionary.GetAttribute(attribute.Name)
                    ?? throw new InvalidConfigurationException($"Unknown attribute '{attribute.Name}' in RadiusReply configuration element, please see dictionary. Config name: '{builder.Name}'");

                if (!replyAttributes.ContainsKey(attribute.Name))
                {
                    replyAttributes.Add(attribute.Name, new List<RadiusReplyAttributeValue>());
                }

                if (!string.IsNullOrEmpty(attribute.From))
                {
                    replyAttributes[attribute.Name].Add(new RadiusReplyAttributeValue(attribute.From, attribute.Sufficient));
                    continue;
                }
                
                try
                {
                    var value = ParseRadiusReplyAttributeValue(radiusAttribute, attribute.Value);
                    replyAttributes[attribute.Name].Add(new RadiusReplyAttributeValue(value, attribute.When, attribute.Sufficient));
                }
                catch (Exception ex)
                {
                    throw new InvalidConfigurationException($"Error while parsing attribute '{radiusAttribute.Name}' with {radiusAttribute.Type} value '{attribute.Value}' in RadiusReply configuration element: {ex.Message}. Config name: '{builder.Name}'");
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
