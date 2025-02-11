using MultiFactor.Radius.Adapter.Core.Radius;
using Multifactor.Radius.Adapter.EndToEndTests.Constants;
using Multifactor.Radius.Adapter.EndToEndTests.Fixtures;

namespace Multifactor.Radius.Adapter.EndToEndTests.Tests;

[Collection("Radius e2e")]
public class DefaultUsersBindTests(RadiusFixtures radiusFixtures) : E2ETestBase(radiusFixtures)
{
    [Fact]
    public async Task SendAuthRequestWithoutCredentials_ShouldReject()
    {
        await StartHostAsync(RadiusAdapterConfigs.RootConfig, [RadiusAdapterConfigs.AccessRequestConfig]);

        var accessRequest = CreateRadiusPacket(PacketCode.AccessRequest);
        accessRequest!.AddAttributes(new Dictionary<string, object>()
            { { "NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier } });

        var response = SendPacketAsync(accessRequest);

        Assert.NotNull(response);
        Assert.Equal(PacketCode.AccessReject, response.Header.Code);
    }
    
    [Theory]
    [InlineData("ad.env")]
    public async Task SendAuthRequestWithBindUser_ShouldAccept(string configName)
    {
        var sensitiveData =
            E2ETestsUtils.GetSensitiveData(configName);
        
        var prefix = E2ETestsUtils.GetEnvPrefix(sensitiveData.First().Key);
        
        await TestEnvironmentVariables.With(async env =>
        {
            env.SetEnvironmentVariables(sensitiveData);
            
            await StartHostAsync(
                RadiusAdapterConfigs.RootConfig,
                [RadiusAdapterConfigs.AccessRequestConfig],
                envPrefix: prefix);

            var accessRequest = CreateRadiusPacket(PacketCode.AccessRequest);
            accessRequest!.AddAttributes(new Dictionary<string, object>()
            {
                { "NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier },
                { "User-Name", RadiusAdapterConstants.BindUserName },
                { "User-Password", RadiusAdapterConstants.BindUserPassword }
            });

            var response = SendPacketAsync(accessRequest);

            Assert.NotNull(response);
            Assert.Equal(PacketCode.AccessAccept, response.Header.Code);
        });
    }
    
    [Theory]
    [InlineData("ad.env")]
    public async Task SendAuthRequestWithAdminUser_ShouldAccept(string configName)
    {
        var sensitiveData =
            E2ETestsUtils.GetSensitiveData(configName);
        
        var prefix = E2ETestsUtils.GetEnvPrefix(sensitiveData.First().Key);
        
        await TestEnvironmentVariables.With(async env =>
        {
            env.SetEnvironmentVariables(sensitiveData);
            
            await StartHostAsync(
                RadiusAdapterConfigs.RootConfig,
                [RadiusAdapterConfigs.AccessRequestConfig],
                envPrefix: prefix);

            var accessRequest = CreateRadiusPacket(PacketCode.AccessRequest);
            accessRequest!.AddAttributes(new Dictionary<string, object>()
            {
                { "NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier },
                { "User-Name", RadiusAdapterConstants.AdminUserName },
                { "User-Password", RadiusAdapterConstants.AdminUserPassword }
            });

            var response = SendPacketAsync(accessRequest);

            Assert.NotNull(response);
            Assert.Equal(PacketCode.AccessAccept, response.Header.Code);
        });
    }
    
    [Theory]
    [InlineData("ad.env")]
    public async Task SendAuthRequestWithPasswordUser_ShouldAccept(string configName)
    {
        var sensitiveData =
            E2ETestsUtils.GetSensitiveData(configName);
        
        var prefix = E2ETestsUtils.GetEnvPrefix(sensitiveData.First().Key);
        
        await TestEnvironmentVariables.With(async env =>
        {
            env.SetEnvironmentVariables(sensitiveData);
            
            await StartHostAsync(
                RadiusAdapterConfigs.RootConfig,
                [RadiusAdapterConfigs.AccessRequestConfig],
                envPrefix: prefix);

            var accessRequest = CreateRadiusPacket(PacketCode.AccessRequest);
            accessRequest!.AddAttributes(new Dictionary<string, object>()
            {
                { "NAS-Identifier", RadiusAdapterConstants.DefaultNasIdentifier },
                { "User-Name", RadiusAdapterConstants.ChangePasswordUserName },
                { "User-Password", RadiusAdapterConstants.ChangePasswordUserPassword }
            });

            var response = SendPacketAsync(accessRequest);

            Assert.NotNull(response);
            Assert.Equal(PacketCode.AccessAccept, response.Header.Code);
        });
    }
}