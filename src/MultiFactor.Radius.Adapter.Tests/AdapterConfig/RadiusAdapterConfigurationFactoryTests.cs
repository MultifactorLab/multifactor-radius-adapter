using MultiFactor.Radius.Adapter.Infrastructure.Configuration.ConfigurationLoading;
using MultiFactor.Radius.Adapter.Tests.Fixtures;

namespace MultiFactor.Radius.Adapter.Tests.AdapterConfig;

public class RadiusAdapterConfigurationFactoryTests
{
    [Fact]
    public void CreateMinimalRoot_WithNoEnvVar_ShouldCreate()
    {
        var path = TestEnvironment.GetAssetPath("root-minimal-single.config");
        var config = RadiusAdapterConfigurationFactory.Create(path);
        
        Assert.Equal("0.0.0.0:1812", config.AppSettings.AdapterServerEndpoint);
        Assert.Equal("000", config.AppSettings.RadiusSharedSecret);
        Assert.Equal("https://api.multifactor.dev", config.AppSettings.MultifactorApiUrl);
        Assert.Equal("None", config.AppSettings.FirstFactorAuthenticationSource);
        Assert.Equal("key", config.AppSettings.MultifactorNasIdentifier);
        Assert.Equal("secret", config.AppSettings.MultifactorSharedSecret);
        Assert.Equal("Debug", config.AppSettings.LoggingLevel);
    }
    
    [Fact]
    public void CreateMinimalRoot_OverrideByEnvVar_ShouldCreate()
    {
        TestEnvironmentVariables.With(env =>
        {
            env.SetEnvironmentVariable("rad_appsettings__adapterServerEndpoint", "0.0.0.0:1818");
            env.SetEnvironmentVariable("rad_appsettings__RadiusSharedSecret", "888");
            env.SetEnvironmentVariable("rad_appsettings__MultifactorApiUrl", "https://api.multifactor.ru");
            env.SetEnvironmentVariable("rad_appsettings__FirstFactorAuthenticationSource", "ActiveDirectory");
            env.SetEnvironmentVariable("rad_appsettings__MultifactorNasIdentifier", "my key");
            env.SetEnvironmentVariable("rad_appsettings__MultifactorSharedSecret", "my secret");
            env.SetEnvironmentVariable("rad_appsettings__LoggingLevel", "Info");

            var path = TestEnvironment.GetAssetPath("root-minimal-single.config");
            var config = RadiusAdapterConfigurationFactory.Create(path);
        
            Assert.Equal("0.0.0.0:1818", config.AppSettings.AdapterServerEndpoint);
            Assert.Equal("888", config.AppSettings.RadiusSharedSecret);
            Assert.Equal("https://api.multifactor.ru", config.AppSettings.MultifactorApiUrl);
            Assert.Equal("ActiveDirectory", config.AppSettings.FirstFactorAuthenticationSource);
            Assert.Equal("my key", config.AppSettings.MultifactorNasIdentifier);
            Assert.Equal("my secret", config.AppSettings.MultifactorSharedSecret);
            Assert.Equal("Info", config.AppSettings.LoggingLevel);
        });
    }
    
    [Fact]
    public void CreateClient_WithNoEnvVar_ShouldCreate()
    {
        var path = TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, 
            "client-minimal-for-overriding.config");
        var config = RadiusAdapterConfigurationFactory.Create(path, "client-minimal-for-overriding");
        
        Assert.Equal("windows", config.AppSettings.RadiusClientNasIdentifier);
        Assert.Equal("000", config.AppSettings.RadiusSharedSecret);
        Assert.Equal("None", config.AppSettings.FirstFactorAuthenticationSource);
        Assert.Equal("key", config.AppSettings.MultifactorNasIdentifier);
        Assert.Equal("secret", config.AppSettings.MultifactorSharedSecret);
    }
    
    [Fact]
    public void CreateClient_OverrideByEnvVar_ShouldCreate()
    {
        TestEnvironmentVariables.With(env =>
        {
            env.SetEnvironmentVariable("rad_client-minimal-for-overriding_appsettings__RadiusClientNasIdentifier", 
                "Linux");
            env.SetEnvironmentVariable("rad_client-minimal-for-overriding_appsettings__RadiusSharedSecret", 
                "888");
            env.SetEnvironmentVariable("rad_client-minimal-for-overriding_appsettings__FirstFactorAuthenticationSource", 
                "ActiveDirectory");
            env.SetEnvironmentVariable("rad_client-minimal-for-overriding_appsettings__MultifactorNasIdentifier", 
                "my key");
            env.SetEnvironmentVariable("rad_client-minimal-for-overriding_appsettings__MultifactorSharedSecret", 
                "my secret");
            
            var path = TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, 
                "client-minimal-for-overriding.config");
            var config = RadiusAdapterConfigurationFactory.Create(path, "client-minimal-for-overriding");
        
            Assert.Equal("Linux", config.AppSettings.RadiusClientNasIdentifier);
            Assert.Equal("888", config.AppSettings.RadiusSharedSecret);
            Assert.Equal("ActiveDirectory", config.AppSettings.FirstFactorAuthenticationSource);
            Assert.Equal("my key", config.AppSettings.MultifactorNasIdentifier);
            Assert.Equal("my secret", config.AppSettings.MultifactorSharedSecret);
        });
    }    
    
    [Fact]
    public void CreateClientWithSpacedName_OverrideByEnvVar_ShouldCreate()
    {
        TestEnvironmentVariables.With(env =>
        {
            env.SetEnvironmentVariable("rad_client_minimal_spaced_appsettings__RadiusClientNasIdentifier", 
                "Linux");
            
            var path = TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, 
                "client-minimal-for-overriding.config");
            var config = RadiusAdapterConfigurationFactory.Create(path, "client minimal spaced");
        
            Assert.Equal("Linux", config.AppSettings.RadiusClientNasIdentifier);
        });
    }
    
    [Fact]
    public void CreateClient_ComplexPathOverrideByEnvVar_ShouldCreate()
    {
        TestEnvironmentVariables.With(env =>
        {
            // Path = RadiusReply:Attributes:add:0:name, Value = Fortinet-Group-Name
            env.SetEnvironmentVariable(
                "rad_client-minimal-for-overriding_RadiusReply__Attributes__add__0__name", 
                "Fortinet-Group-Name");
            env.SetEnvironmentVariable(
                "rad_client-minimal-for-overriding_RadiusReply__Attributes__add__0__value", 
                "Users");
            
            var path = TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, 
                "client-minimal-for-overriding.config");
            var config = RadiusAdapterConfigurationFactory.Create(path, "client-minimal-for-overriding");
            var attribute = Assert.Single(config.RadiusReply.Attributes.Elements);
            Assert.NotNull(attribute);
            
            Assert.Equal("Fortinet-Group-Name", attribute.Name);
            Assert.Equal("Users", attribute.Value);
        });
    }
}