using Microsoft.Extensions.DependencyInjection;
using MultiFactor.Radius.Adapter.Services;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;

namespace MultiFactor.Radius.Adapter.Tests;

public class DataProtectionServiceTests
{
    [Fact]
    public void ProtectAndUnprotectWithSameProtector_ShouldBeSamePassword()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
            });
        });
        
        var dataProtectionService = host.Service<DataProtectionService>();
        
        var password = "password";
        var encrypted = dataProtectionService.Protect(password);
        Assert.True(!string.IsNullOrWhiteSpace(encrypted));
        
        var decrypted = dataProtectionService.Unprotect(encrypted);
        Assert.Equal(password, decrypted);
    }
}