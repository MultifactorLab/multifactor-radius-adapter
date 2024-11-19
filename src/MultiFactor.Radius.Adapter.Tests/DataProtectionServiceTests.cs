using System.Security.Cryptography;
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
        var protector = "test-protector";
        var encrypted = dataProtectionService.Protect(password, protector);
        Assert.True(!string.IsNullOrWhiteSpace(encrypted));
        
        var decrypted = dataProtectionService.Unprotect(encrypted, protector);
        Assert.Equal(password, decrypted);
    }
    
    [Fact]
    public void ProtectAndUnprotectWithDifferentProtector_ShouldThrowCryptographicException()
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
        var protector = "test-protector";
        var encrypted = dataProtectionService.Protect(password, protector);
        Assert.True(!string.IsNullOrWhiteSpace(encrypted));
        
        var anotherProtector = "another-protector";
        Assert.Throws<CryptographicException>(() => dataProtectionService.Unprotect(encrypted, anotherProtector));
    }
}