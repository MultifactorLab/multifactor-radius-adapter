using MultiFactor.Radius.Adapter.Tests.Fixtures;

namespace MultiFactor.Radius.Adapter.Tests.AdapterConfig;

[Trait("Category", "Adapter Configuration")]
[Trait("Category", "App config Reading")]
public class AppConfigConfiurationSourceTests
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
}
