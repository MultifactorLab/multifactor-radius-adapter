using Microsoft.Extensions.Configuration;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.ConfigurationLoading;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.XmlAppConfiguration;
using MultiFactor.Radius.Adapter.Tests.Fixtures;

namespace MultiFactor.Radius.Adapter.Tests.AdapterConfig;

[Trait("Category", "Adapter Configuration")]
[Trait("Category", "App config Reading")]
public class AppConfigConfigurationSourceTests
{
    [Fact]
    public void Load_ShouldLoadAndTransformNames()
    {
        var path = TestEnvironment.GetAssetPath("root-all-appsettings-items.config");
        var source = new TestableAppConfigConfigurationSource(path);

        source.Load();

        Assert.Equal("https://api.multifactor.dev", source.AllData["appSettings:MultifactorApiUrl"]);
        Assert.Equal("http://proxy.domain.ru", source.AllData["appSettings:MultifactorApiProxy"]);
        Assert.Equal("30", source.AllData["appSettings:MultifactorApiTimeout"]);
        Assert.Equal("windows", source.AllData["appSettings:MultifactorNasIdentifier"]);
        Assert.Equal("secret", source.AllData["appSettings:MultifactorSharedSecret"]);
        Assert.Equal("group1;group2", source.AllData["appSettings:SignUpGroups"]);
        Assert.Equal("true", source.AllData["appSettings:BypassSecondFactorWhenApiUnreachable"]);

        Assert.Equal("ActiveDirectory", source.AllData["appSettings:FirstFactorAuthenticationSource"]);

        Assert.Equal("domain.local", source.AllData["appSettings:ActiveDirectoryDomain"]);
        Assert.Equal("BypassGroup", source.AllData["appSettings:ActiveDirectory2faBypassGroup"]);
        Assert.Equal("2faGroup", source.AllData["appSettings:ActiveDirectory2faGroup"]);
        Assert.Equal("AdGroup", source.AllData["appSettings:ActiveDirectoryGroup"]);
        Assert.Equal("cn=users", source.AllData["appSettings:LdapBindDn"]);
        Assert.Equal("true", source.AllData["appSettings:LoadActiveDirectoryNestedGroups"]);
        Assert.Equal("true", source.AllData["appSettings:UseActiveDirectoryMobileUserPhone"]);
        Assert.Equal("true", source.AllData["appSettings:UseActiveDirectoryUserPhone"]);
        Assert.Equal("true", source.AllData["appSettings:UseUpnAsIdentity"]);
        Assert.Equal("attr", source.AllData["appSettings:UseAttributeAsIdentity"]);
        Assert.Equal("mobilephone", source.AllData["appSettings:PhoneAttribute"]);
        Assert.Equal("pwd", source.AllData["appSettings:ServiceAccountPassword"]);
        Assert.Equal("usr", source.AllData["appSettings:ServiceAccountUser"]);

        Assert.Equal("10.10.10.10", source.AllData["appSettings:AdapterClientEndpoint"]);
        Assert.Equal("0.0.0.0:1812", source.AllData["appSettings:AdapterServerEndpoint"]);
        Assert.Equal("156.120.120.4", source.AllData["appSettings:NpsServerEndpoint"]);
        Assert.Equal("10.10.10.2", source.AllData["appSettings:RadiusClientIp"]);
        Assert.Equal("cli-nas", source.AllData["appSettings:RadiusClientNasIdentifier"]);
        Assert.Equal("888", source.AllData["appSettings:RadiusSharedSecret"]);

        Assert.Equal("json", source.AllData["appSettings:LoggingFormat"]);
        Assert.Equal("Debug", source.AllData["appSettings:LoggingLevel"]);
        Assert.Equal("csid", source.AllData["appSettings:CallingStationIdAttribute"]);
        Assert.Equal("consoletempl", source.AllData["appSettings:ConsoleLogOutputTemplate"]);
        Assert.Equal("filetempl", source.AllData["appSettings:FileLogOutputTemplate"]);
        Assert.Equal("15", source.AllData["appSettings:InvalidCredentialDelay"]);

        Assert.Equal("Full", source.AllData["appSettings:PrivacyMode"]);
        Assert.Equal("otp", source.AllData["appSettings:PreAuthenticationMethod"]);
        Assert.Equal("10", source.AllData["appSettings:AuthenticationCacheLifetime"]);
        Assert.Equal("false", source.AllData["appSettings:AuthenticationCacheMinimalMatching"]);
    }
    
    [Fact]
    public void Get_ShouldBindAndAllNestedElementsNotBeNull()
    {
        var path = TestEnvironment.GetAssetPath("root-minimal-multi.config");

        var config = new ConfigurationBuilder()
            .Add(new XmlAppConfigurationSource(path))
            .Build();

        var bound = config.BindRadiusAdapterConfig();

        Assert.NotNull(bound?.AppSettings);

        Assert.NotNull(bound.RadiusReply?.Attributes?.Elements);
        Assert.Empty(bound.RadiusReply.Attributes.Elements);

        Assert.NotNull(bound.UserNameTransformRules);
        Assert.Empty(bound.UserNameTransformRules.Elements);
    }
    
    [Fact]
    [Trait("Category", "Radius Reply Attributes")]
    public void Get_ShouldBindRadiusReplySection()
    {
        var path = TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "radius-reply-join.config");

        var config = new ConfigurationBuilder()
            .Add(new XmlAppConfigurationSource(path))
            .Build();

        var bound = config.BindRadiusAdapterConfig();

        Assert.Equal(2, bound!.RadiusReply.Attributes.Elements.Length);

        Assert.Contains(bound.RadiusReply.Attributes.Elements, x =>
        {
            return x.Name == "Fortinet-Group-Name" && 
                x.Value == "Users" && 
                x.When == "UserGroup=VPN Users" && 
                x.Sufficient;
        });
        
        Assert.Contains(bound.RadiusReply.Attributes.Elements, x =>
        {
            return x.Name == "Fortinet-Group-Name" && 
                x.Value == "Admins" && 
                x.When == "UserGroup=VPN Admins" && 
                !x.Sufficient;
        });
    }
    
    [Fact]
    [Trait("Category", "User Name Transform Rules")]
    public void Get_Single_ShouldBindRadiusReplySection()
    {
        var path = TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "radius-reply-single.config");

        var config = new ConfigurationBuilder()
            .Add(new XmlAppConfigurationSource(path))
            .Build();

        var bound = config.BindRadiusAdapterConfig();

        var attribute = Assert.Single(bound.RadiusReply.Attributes.Elements);
        Assert.Equal("Fortinet-Group-Name", attribute.Name);
        Assert.Equal("Users", attribute.Value);
        Assert.Equal("UserGroup=VPN Users", attribute.When);
        Assert.True(attribute.Sufficient);
    }
    
    [Fact]
    [Trait("Category", "User Name Transform Rules")]
    public void Get_ShouldBindUserNameTransformRulesSection()
    {
        var path = TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "user-name-transform-rules.config");

        var config = new ConfigurationBuilder()
            .Add(new XmlAppConfigurationSource(path))
            .Build();

        var bound = config.BindRadiusAdapterConfig();

        Assert.Equal(2, bound!.UserNameTransformRules.Elements.Count());

        Assert.Contains(bound.UserNameTransformRules.Elements, x =>
        {
            return x.Match == "^([^@]*)$" &&
                x.Replace == "$1@domain.local" &&
                x.Count == 3;
        });
        
        Assert.Contains(bound!.UserNameTransformRules.Elements, x =>
        {
            return x.Match == "^([^@]*)$" &&
                x.Replace == "$1@domain.local" &&
                x.Count == null;
        });
    }

    [Fact]
    [Trait("Category", "User Name Transform Rules")]
    public void Get_SingleRule_ShouldBindUserNameTransformRulesSection()
    {
        var path = TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "user-name-transform-single-rule.config");

        var config = new ConfigurationBuilder()
            .Add(new XmlAppConfigurationSource(path))
            .Build();

        var bound = config.BindRadiusAdapterConfig();

        var rule = Assert.Single(bound.UserNameTransformRules.Elements);
        Assert.Equal("^([^@]*)$", rule.Match);
        Assert.Equal("$1@domain.local", rule.Replace);
        Assert.Equal(3, rule.Count);
    }
    
    [Fact]
    [Trait("Category", "bypass-second-factor-when-api-unreachable")]
    public void Get_BypassSecondFactorWhenApiUnreachableShouldBeTrueByDefault()
    {
        var path = TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "user-name-transform-rules.config");

        var config = new ConfigurationBuilder()
            .Add(new XmlAppConfigurationSource(path))
            .Build();

        var bound = config.BindRadiusAdapterConfig();

        Assert.True(bound!.AppSettings.BypassSecondFactorWhenApiUnreachable);
    }
}
