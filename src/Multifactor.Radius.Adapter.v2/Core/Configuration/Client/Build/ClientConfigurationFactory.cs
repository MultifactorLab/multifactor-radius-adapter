using System.Net;
using System.Text.RegularExpressions;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.Auth.PreAuthMode;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Service;
using Multifactor.Radius.Adapter.v2.Core.MultifactorApi.PrivacyMode;
using Multifactor.Radius.Adapter.v2.Core.Radius.Attributes;
using Multifactor.Radius.Adapter.v2.Core.RandomWaiterFeature;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.Exceptions;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Sections;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Sections.LdapServer;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Sections.RadiusReply;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Sections.UserNameTransform;

namespace Multifactor.Radius.Adapter.v2.Core.Configuration.Client.Build;

public class ClientConfigurationFactory : IClientConfigurationFactory
{
    private readonly IRadiusDictionary _dictionary;

    public ClientConfigurationFactory(IRadiusDictionary dictionary)
    {
        _dictionary = dictionary;
    }

    public IClientConfiguration CreateConfig(
        string name,
        RadiusAdapterConfiguration configuration,
        IServiceConfiguration serviceConfig)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
        }

        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(serviceConfig);

        var appSettings = configuration.AppSettings;
        ValidateAppSettings(appSettings, name);

        var firstFactorAuthenticationSource = Enum.Parse<AuthenticationSource>(
            appSettings.FirstFactorAuthenticationSource,
            true);

        var builder = new ClientConfiguration(
            name,
            appSettings.RadiusSharedSecret,
            firstFactorAuthenticationSource,
            appSettings.MultifactorNasIdentifier,
            appSettings.MultifactorSharedSecret);

        builder.SetBypassSecondFactorWhenApiUnreachable(appSettings.BypassSecondFactorWhenApiUnreachable);

        ReadPrivacyModeSetting(appSettings, builder);
        ReadInvalidCredDelaySetting(appSettings, builder, serviceConfig);
        ReadPreAuthModeSetting(appSettings, builder);

        if (builder.FirstFactorAuthenticationSource == AuthenticationSource.Radius)
            ReadRadiusAuthenticationSourceSettings(builder, appSettings);

        ReadLdapServersSettings(builder, configuration.LdapServers);
        ReadRadiusReplyAttributes(builder, _dictionary, configuration.RadiusReply);

        LoadUserNameTransformRulesSection(configuration, builder);

        ReadSignUpGroupsSettings(builder, appSettings);
        ReadAuthenticationCacheSettings(appSettings, builder);

        var callingStationIdAttr = appSettings.CallingStationIdAttribute;
        if (!string.IsNullOrWhiteSpace(callingStationIdAttr))
        {
            builder.SetCallingStationIdVendorAttribute(callingStationIdAttr);
        }

        return builder;
    }

    private static void ReadLdapServersSettings(ClientConfiguration builder, LdapServersSection ldapServersSection)
    {
        if (builder.FirstFactorAuthenticationSource == AuthenticationSource.Ldap)
        {
            if (ldapServersSection.Servers.Length == 0)
                throw InvalidConfigurationException.For(
                    x => x.LdapServers,
                    "Can't parse '{prop}' value. Config name: '{0}'",
                    builder.Name);
        }
        else
        {
            if (ldapServersSection.Servers.Length == 0)
                return;
        }

        ValidateLdapServers(ldapServersSection, builder.Name);
        
        foreach (var ldapSettings in ldapServersSection.Servers)
        {
            var ldapConfig = new LdapServerConfiguration(
                ldapSettings.ConnectionString,
                ldapSettings.UserName,
                ldapSettings.Password);

            ldapConfig
                .AddPhoneAttributes(Utils.SplitString(ldapSettings.PhoneAttributes))
                .AddAccessGroups(Utils.SplitString(ldapSettings.AccessGroups))
                .AddSecondFaGroups(Utils.SplitString(ldapSettings.SecondFaGroups))
                .AddSecondFaBypassGroups(Utils.SplitString(ldapSettings.SecondFaBypassGroups))
                .AddNestedGroupBaseDns(Utils.SplitString(ldapSettings.NestedGroupsBaseDn))
                .SetIdentityAttribute(ldapSettings.IdentityAttribute)
                .SetLoadNestedGroups(ldapSettings.LoadNestedGroups)
                .SetBindTimeoutInSeconds(ldapSettings.BindTimeoutInSeconds);

            builder.AddLdapServers(ldapConfig);
        }
    }

    private static void ReadInvalidCredDelaySetting(
        AppSettingsSection appSettings,
        ClientConfiguration builder,
        IServiceConfiguration serviceConfig)
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
            throw InvalidConfigurationException.For(
                x => x.AppSettings.InvalidCredentialDelay,
                "Can't parse '{prop}' value. Config name: '{0}'",
                builder.Name);
        }
    }

    private static void ReadPreAuthModeSetting(AppSettingsSection appSettings, ClientConfiguration builder)
    {
        try
        {
            builder.SetPreAuthMode(PreAuthModeDescriptor.Create(appSettings.PreAuthenticationMethod,
                PreAuthModeSettings.Default));
        }
        catch
        {
            throw InvalidConfigurationException.For(
                x => x.AppSettings.PreAuthenticationMethod,
                "Can't parse '{prop}' value. Must be one of: {0}. Config name: '{1}'",
                PreAuthModeDescriptor.DisplayAvailableModes(),
                builder.Name);
        }

        if (builder.PreAuthnMode.Mode != PreAuthMode.None && builder.InvalidCredentialDelay.Min < 2)
        {
            throw InvalidConfigurationException.For(
                x => x.AppSettings.InvalidCredentialDelay,
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
            throw InvalidConfigurationException.For(
                x => x.AppSettings.PrivacyMode,
                "Can't parse '{prop}' value. Must be one of: Full, None, Partial:Field1,Field2. Config name: '{0}'",
                builder.Name);
        }
    }

    private static void LoadUserNameTransformRulesSection(RadiusAdapterConfiguration configuration, ClientConfiguration builder)
    {
        var userNameTransformRulesSection = configuration.UserNameTransformRules;
        var firstFactorRules = new List<UserNameTransformRule>();
        var secondFactorRules = new List<UserNameTransformRule>();

        if (userNameTransformRulesSection?.Elements?.Length > 0)
        {
            firstFactorRules.AddRange(userNameTransformRulesSection.Elements);
            secondFactorRules.AddRange(userNameTransformRulesSection.Elements);
        }

        firstFactorRules.AddRange(userNameTransformRulesSection?.BeforeFirstFactor?.Elements ?? []);
        secondFactorRules.AddRange(userNameTransformRulesSection?.BeforeSecondFactor?.Elements ?? []);

        builder.SetUserNameTransformRules(
            new UserNameTransformRules(firstFactorRules, secondFactorRules)
        );
    }

    private static void ReadRadiusAuthenticationSourceSettings(ClientConfiguration builder, AppSettingsSection appSettings)
    {
        if (string.IsNullOrWhiteSpace(appSettings.AdapterClientEndpoint))
        {
            throw InvalidConfigurationException.For(
                x => x.AppSettings.AdapterClientEndpoint,
                "'{prop}' element not found. Config name: '{0}'",
                builder.Name);
        }

        if (string.IsNullOrWhiteSpace(appSettings.NpsServerEndpoint))
        {
            throw InvalidConfigurationException.For(
                x => x.AppSettings.NpsServerEndpoint,
                "'{prop}' element not found. Config name: '{0}'",
                builder.Name);
        }

        if (!IPEndPointFactory.TryParse(appSettings.AdapterClientEndpoint, out var serviceClientEndpoint))
        {
            throw InvalidConfigurationException.For(
                x => x.AppSettings.AdapterClientEndpoint,
                "Can't parse '{prop}' value. Config name: '{0}'",
                builder.Name);
        }

        if (!IPEndPointFactory.TryParse(appSettings.NpsServerEndpoint, out var npsEndpoint))
        {
            throw InvalidConfigurationException.For(
                x => x.AppSettings.NpsServerEndpoint,
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
            throw InvalidConfigurationException.For(
                x => x.AppSettings.SignUpGroups,
                "Invalid group names. Please check '{prop}' settings property and fix syntax errors. Config name: '{0}'",
                builder.Name);
        }

        builder.SetSignUpGroups(signUpGroupsSettings);
    }

    private static void ReadAuthenticationCacheSettings(AppSettingsSection appSettings, ClientConfiguration builder)
    {
        try
        {
            var ltConf = AuthenticatedClientCacheConfig.Create(
                appSettings.AuthenticationCacheLifetime,
                appSettings.AuthenticationCacheMinimalMatching);
            builder.SetAuthenticationCacheLifetime(ltConf);
        }
        catch
        {
            throw InvalidConfigurationException.For(
                x => x.AppSettings.AuthenticationCacheLifetime,
                "Can't parse '{prop}' value. Config name: '{0}'",
                builder.Name);
        }
    }

    private static void ReadRadiusReplyAttributes(
        ClientConfiguration builder,
        IRadiusDictionary dictionary,
        RadiusReplySection? radiusReplyAttributesSection)
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

                if (!string.IsNullOrWhiteSpace(attribute.From))
                {
                    replyAttributes[attribute.Name]
                        .Add(new RadiusReplyAttributeValue(attribute.From, attribute.Sufficient));
                    continue;
                }

                try
                {
                    var value = ParseRadiusReplyAttributeValue(radiusAttribute, attribute.Value);
                    replyAttributes[attribute.Name]
                        .Add(new RadiusReplyAttributeValue(value, attribute.When, attribute.Sufficient));
                }
                catch (Exception ex)
                {
                    throw new InvalidConfigurationException(
                        $"Error while parsing attribute '{radiusAttribute.Name}' with {radiusAttribute.Type} value '{attribute.Value}' in RadiusReply configuration element: {ex.Message}. Config name: '{builder.Name}'");
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
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new Exception("Value must be specified");
        }

        return attribute.Type switch
        {
            DictionaryAttribute.TypeString or DictionaryAttribute.TypeTaggedString => value,
            DictionaryAttribute.TypeInteger or DictionaryAttribute.TypeTaggedInteger => uint.Parse(value),
            DictionaryAttribute.TypeIpAddr => IPAddress.Parse(value),
            DictionaryAttribute.TypeOctet => Utils.StringToByteArray(value),
            _ => throw new Exception($"Unknown type {attribute.Type}")
        };
    }

    private void ValidateAppSettings(AppSettingsSection appSettings, string configName)
    {
        if (string.IsNullOrWhiteSpace(appSettings.FirstFactorAuthenticationSource))
        {
            throw InvalidConfigurationException.For(
                x => x.AppSettings.FirstFactorAuthenticationSource,
                "'{prop}' element not found. Config name: '{0}'",
                configName);
        }

        var isDigit = int.TryParse(appSettings.FirstFactorAuthenticationSource, out _);
        var isValidAuthSource =
            Enum.TryParse<AuthenticationSource>(appSettings.FirstFactorAuthenticationSource, true, out _);
        var authTypes = Enum.GetNames<AuthenticationSource>();
        
        if (isDigit || !isValidAuthSource)
        {
            throw InvalidConfigurationException.For(
                x => x.AppSettings.FirstFactorAuthenticationSource,
                "Can't parse '{prop}' value. Must be one of: {1}. Config name: '{0}'",
                configName,
                string.Join(", ", authTypes));
        }

        if (string.IsNullOrWhiteSpace(appSettings.RadiusSharedSecret))
        {
            throw InvalidConfigurationException.For(
                x => x.AppSettings.RadiusSharedSecret,
                "'{prop}' element not found. Config name: '{0}'",
                configName);
        }

        if (string.IsNullOrWhiteSpace(appSettings.MultifactorNasIdentifier))
        {
            throw InvalidConfigurationException.For(
                x => x.AppSettings.MultifactorNasIdentifier,
                "'{prop}' element not found. Config name: '{0}'",
                configName);
        }

        if (string.IsNullOrWhiteSpace(appSettings.MultifactorSharedSecret))
        {
            throw InvalidConfigurationException.For(
                x => x.AppSettings.MultifactorSharedSecret,
                "'{prop}' element not found. Config name: '{0}'",
                configName);
        }
    }

    private static void ValidateLdapServers(LdapServersSection section, string configName)
    {
        foreach (var server in section.Servers)
        {
            if (string.IsNullOrWhiteSpace(server.ConnectionString))
            {
                throw InvalidConfigurationException.For(
                    x => server.ConnectionString,
                    "Can't parse '{prop}' value. Config name: '{0}'",
                    configName);
            }

            if (string.IsNullOrWhiteSpace(server.Password))
            {
                throw InvalidConfigurationException.For(
                    x => server.Password,
                    "Can't parse '{prop}' value. Config name: '{0}'",
                    configName);
            }
            
            if (string.IsNullOrWhiteSpace(server.UserName))
            {
                throw InvalidConfigurationException.For(
                    x => server.UserName,
                    "Can't parse '{prop}' value. Config name: '{0}'",
                    configName);
            }
        }
    }
}