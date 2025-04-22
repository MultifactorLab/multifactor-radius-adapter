using Microsoft.Extensions.Configuration;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.ConfigurationLoading;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.Sections.LdapServer;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.XmlAppConfiguration;

namespace Multifactor.Radius.Adapter.v2.Tests.ConfigurationTests;

public class LoadXmlConfigurationTests
{
    [Fact]
    public void LoadXmlConfig_ShouldLoadLdapServersSection()
    {
        var fileName = "full-single-file.config";

        var configRoot = new ConfigurationBuilder()
            .Add(new XmlAppConfigurationSource(TestEnvironment.GetAssetPath(fileName)))
            .Build();

        var config = configRoot.BindRadiusAdapterConfig();

        Assert.NotNull(config);
        Assert.NotNull(config.LdapServers);
        Assert.NotNull(config.LdapServers.LdapServer);
        Assert.NotEmpty(config.LdapServers.LdapServer);
        Assert.Equal(2, config.LdapServers.LdapServer.Length);

        Assert.Contains(config.LdapServers.LdapServer, x =>
        {
            return
                x.ConnectionString == "connection-string" &&
                x.UserName == "username" &&
                x.Password == "password" &&
                x.BindTimeoutInSeconds == 10 &&
                x.AccessGroups == "access-groups" &&
                x.SecondFaGroups == "2fa-groups" &&
                x.SecondFaBypassGroups == "2fa-bypass-groups" &&
                x.LoadNestedGroups == false &&
                x.NestedGroupsBaseDn == "nested-groups-base-dn" &&
                x.PhoneAttributes == "phone-attributes" &&
                x.AttributesAsIdentity == "attributes-as-identity";
        });

        Assert.Contains(config.LdapServers.LdapServer, x =>
        {
            return
                x.ConnectionString == "connection-string" &&
                x.UserName == "username" &&
                x.Password == "password" &&
                x.BindTimeoutInSeconds == 10 &&
                x.AccessGroups == "access-groups" &&
                x.SecondFaGroups == "2fa-groups" &&
                x.SecondFaBypassGroups == "2fa-bypass-groups" &&
                x.LoadNestedGroups == true &&
                x.NestedGroupsBaseDn == "nested-groups-base-dn" &&
                x.PhoneAttributes == "phone-attributes" &&
                x.AttributesAsIdentity == "attributes-as-identity";
        });
    }

    [Fact]
    public void LoadXmlConfig_ShouldLoadAppSettingsSection()
    {
        var fileName = "full-single-file.config";
        var configRoot = new ConfigurationBuilder()
            .Add(new XmlAppConfigurationSource(TestEnvironment.GetAssetPath(fileName)))
            .Build();

        var config = configRoot.BindRadiusAdapterConfig();

        Assert.NotNull(config);
        Assert.NotNull(config.AppSettings);

        Assert.Equal("first-factor-authentication-source", config.AppSettings.FirstFactorAuthenticationSource);
        Assert.Equal("radius-shared-secret", config.AppSettings.RadiusSharedSecret);
        Assert.Equal("multifactor-nas-identifier", config.AppSettings.MultifactorNasIdentifier);
        Assert.Equal("multifactor-shared-secret", config.AppSettings.MultifactorSharedSecret);
        Assert.Equal("adapter-client-endpoint", config.AppSettings.AdapterClientEndpoint);
        Assert.Equal("adapter-server-endpoint", config.AppSettings.AdapterServerEndpoint);
        Assert.Equal("nps-server-endpoint", config.AppSettings.NpsServerEndpoint);
        Assert.Equal("radius-client-ip", config.AppSettings.RadiusClientIp);
        Assert.Equal("radius-client-nas-identifier", config.AppSettings.RadiusClientNasIdentifier);
        Assert.Equal("privacy-mode", config.AppSettings.PrivacyMode);
        Assert.Equal("pre-authentication-method", config.AppSettings.PreAuthenticationMethod);
        Assert.Equal("authentication-cache-lifetime", config.AppSettings.AuthenticationCacheLifetime);
        Assert.True(config.AppSettings.AuthenticationCacheMinimalMatching);
        Assert.Equal("invalid-credential-delay", config.AppSettings.InvalidCredentialDelay);
        Assert.Equal("calling-station-id-attribute", config.AppSettings.CallingStationIdAttribute);
        Assert.Equal("multifactor-api-url", config.AppSettings.MultifactorApiUrl);
        Assert.Equal("multifactor-api-proxy", config.AppSettings.MultifactorApiProxy);
        Assert.Equal("multifactor-api-timeout", config.AppSettings.MultifactorApiTimeout);
        Assert.Equal("sign-up-groups", config.AppSettings.SignUpGroups);
        Assert.True(config.AppSettings.BypassSecondFactorWhenApiUnreachable);
        Assert.Equal("logging-level", config.AppSettings.LoggingLevel);
        Assert.Equal("logging-format", config.AppSettings.LoggingFormat);
        Assert.Equal("console-log-output-template", config.AppSettings.ConsoleLogOutputTemplate);
        Assert.Equal("file-log-output-template", config.AppSettings.FileLogOutputTemplate);
    }

    [Fact]
    public void LoadXmlConfig_ShouldLoadRadiusReplyAttributes()
    {
        var fileName = "full-single-file.config";
        var configRoot = new ConfigurationBuilder()
            .Add(new XmlAppConfigurationSource(TestEnvironment.GetAssetPath(fileName)))
            .Build();

        var config = configRoot.BindRadiusAdapterConfig();

        Assert.Equal(2, config!.RadiusReply.Attributes.Elements.Length);

        Assert.Contains(config.RadiusReply.Attributes.Elements, x =>
        {
            return x.Name == "Fortinet-Group-Name" &&
                   x.Value == "Users" &&
                   x.When == "UserGroup=VPN Users" &&
                   x.Sufficient &&
                   x.From == "from";
        });

        Assert.Contains(config.RadiusReply.Attributes.Elements, x =>
        {
            return x.Name == "Fortinet-Group-Name" &&
                   x.Value == "Admins" &&
                   x.When == "UserGroup=VPN Admins" &&
                   !x.Sufficient &&
                   x.From == "from";
        });
    }

    [Fact]
    public void LoadXmlConfig_ShouldLoadUserNameTransformRules()
    {
        var fileName = "full-single-file.config";

        var configRoot = new ConfigurationBuilder()
            .Add(new XmlAppConfigurationSource(TestEnvironment.GetAssetPath(fileName)))
            .Build();

        var config = configRoot.BindRadiusAdapterConfig();
        Assert.Equal(2, config!.UserNameTransformRules.Elements.Count());

        Assert.Contains(config.UserNameTransformRules.Elements, x =>
        {
            return x.Match == "^([^@]*)$" &&
                   x.Replace == "$1@domain.local" &&
                   x.Count == 3;
        });

        Assert.Contains(config!.UserNameTransformRules.Elements, x =>
        {
            return x.Match == "^([^@]*)$" &&
                   x.Replace == "$1@domain.local" &&
                   x.Count == null;
        });
    }
}