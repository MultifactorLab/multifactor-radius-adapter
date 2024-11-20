using System.Net;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using MultiFactor.Radius.Adapter.Core;
using MultiFactor.Radius.Adapter.Core.Framework.Context;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.ClientLevel;
using MultiFactor.Radius.Adapter.Server;
using MultiFactor.Radius.Adapter.Server.Pipeline.AccessChallenge;
using MultiFactor.Radius.Adapter.Services;
using MultiFactor.Radius.Adapter.Services.Ldap;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;
using MultiFactor.Radius.Adapter.Tests.Fixtures.Radius;

namespace MultiFactor.Radius.Adapter.Tests;

public class ChangePasswordChallengeProcessorTests
{
    [Fact]
    public void AddChallengeContext_ShouldAddChallengeContext()
    {
        const string reqId = "RequestId";

        var client = new ClientConfiguration("cli_config", "rds", AuthenticationSource.None, "key", "secret");
        var request = RadiusPacketFactory.AccessRequest();
        request.AddAttribute("User-Password", "password");

        var context = new RadiusContext(request, client, new Mock<IServiceProvider>().Object)
        {
            RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636),
        };
        context.SetMessageState(reqId);
        
        var mockMemoryCache = new Mock<IMemoryCache>();
        var cacheEntry = Mock.Of<ICacheEntry>();
        mockMemoryCache
            .Setup(m => m.CreateEntry(It.IsAny<object>()))
            .Returns(cacheEntry);

        var ldapService = new Mock<ILdapService>();
        var dataProtectionProviderMock = new Mock<IDataProtectionProvider>();
        dataProtectionProviderMock.Setup(x => x.CreateProtector(It.IsAny<string>())).Returns(Mock.Of<IDataProtector>());
        
        var dataProtectionServiceMock = new DataProtectionService(dataProtectionProviderMock.Object);
        var processor = new ChangePasswordChallengeProcessor(mockMemoryCache.Object, ldapService.Object, dataProtectionServiceMock, new Mock<ILogger<ChangePasswordChallengeProcessor>>().Object);

        processor.AddChallengeContext(context);

        mockMemoryCache.Verify(c => c.CreateEntry(It.IsAny<string>()), Times.Once);
        Assert.True(Guid.TryParse(context.State, out Guid result));
        Assert.True(result != Guid.Empty);
    }

    [Fact]
    public void HasChallengeContext_ShouldReturnTrue()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
            });
        });
        
        var memoryCache = host.Service<IMemoryCache>();
        var ldapService = new Mock<ILdapService>();
        var dataProtectionProvider = host.Service<IDataProtectionProvider>();
        var dataProtectionService = new DataProtectionService(dataProtectionProvider);
        var processor = new ChangePasswordChallengeProcessor(memoryCache, ldapService.Object, dataProtectionService, new Mock<ILogger<ChangePasswordChallengeProcessor>>().Object);

        var request = RadiusPacketFactory.AccessRequest();
        request.AddAttribute("User-Password", "password");
        var client = new ClientConfiguration("cli_config", "rds", AuthenticationSource.None, "key", "secret");
        var context = new RadiusContext(request, client, new Mock<IServiceProvider>().Object)
        {
            RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636),
        };
        var identifier = processor.AddChallengeContext(context);

        Assert.NotNull(identifier);
        Assert.True(processor.HasChallengeContext(identifier));
    }

    [Fact]
    public void HasChallengeContext_ShouldReturnFalse()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
            });
        });
        
        var memoryCache = host.Service<IMemoryCache>();
        var ldapService = new Mock<ILdapService>();
        var dataProtectionProvider = host.Service<IDataProtectionProvider>();
        var dataProtectionService = new DataProtectionService(dataProtectionProvider);
        
        var processor = new ChangePasswordChallengeProcessor(memoryCache, ldapService.Object, dataProtectionService, new Mock<ILogger<ChangePasswordChallengeProcessor>>().Object);

        var identifier = new ChallengeIdentifier("test", "test");

        Assert.False(processor.HasChallengeContext(identifier));
    }

    [Fact]
    public async Task ProcessChallengeAsync_NoPasswordChangeRequest_ShouldReturnAccept()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
            });
        });
        
        var memoryCache = host.Service<IMemoryCache>();
        var ldapService = new Mock<ILdapService>();
        var dataProtectionProvider = host.Service<IDataProtectionProvider>();
        var dataProtectionService = new DataProtectionService(dataProtectionProvider);
        var processor = new ChangePasswordChallengeProcessor(memoryCache, ldapService.Object, dataProtectionService, new Mock<ILogger<ChangePasswordChallengeProcessor>>().Object);

        var request = RadiusPacketFactory.AccessRequest();
        request.AddAttribute("User-Password", "password");

        var client = new ClientConfiguration("cli_config", "rds", AuthenticationSource.None, "key", "secret");
        var context = new RadiusContext(request, client, new Mock<IServiceProvider>().Object)
        {
            RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636),
        };

        context.SetMessageState("State");

        var identifier = new ChallengeIdentifier("test", "test");
        var resultCode = await processor.ProcessChallengeAsync(identifier, context);

        Assert.Equal(ChallengeCode.Accept, resultCode);
    }

    [Fact]
    public async Task ProcessChallengeAsync_NoPasswordInContext_ShouldReturnReject()
    {
        var request = RadiusPacketFactory.AccessRequest();

        var client = new ClientConfiguration("cli_config", "rds", AuthenticationSource.None, "key", "secret");
        var context = new RadiusContext(request, client, new Mock<IServiceProvider>().Object)
        {
            RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636),
        };

        context.SetMessageState("State");

        var identifier = new ChallengeIdentifier("test", "test");
        context.SetMessageState(identifier.RequestId);

        var mockMemoryCache = new Mock<IMemoryCache>();
        object passwordChangeRequest = new PasswordChangeRequest();

        mockMemoryCache
            .Setup(mc => mc.TryGetValue(It.IsAny<string>(), out passwordChangeRequest))
            .Returns(true);

        var ldapService = new Mock<ILdapService>();
        var dataProtectionProviderMock = new Mock<IDataProtectionProvider>();
        var dataProtectionServiceMock = new DataProtectionService(dataProtectionProviderMock.Object);
        var processor = new ChangePasswordChallengeProcessor(mockMemoryCache.Object, ldapService.Object, dataProtectionServiceMock, new Mock<ILogger<ChangePasswordChallengeProcessor>>().Object);

        var resultCode = await processor.ProcessChallengeAsync(identifier, context);

        Assert.Equal(ChallengeCode.Reject, resultCode);
    }

    [Fact]
    public async Task ProcessChallengeAsync_NoNewPassword_ShouldReturnInProgressStatusAndUpdateState()
    {
        var ldapService = new Mock<ILdapService>();
        object passwordChangeRequest = new PasswordChangeRequest();
        var passwordChangeRequestId = ((PasswordChangeRequest)passwordChangeRequest).Id;
        var mockMemoryCache = new Mock<IMemoryCache>();
        mockMemoryCache
            .Setup(mc => mc.TryGetValue(It.IsAny<string>(), out passwordChangeRequest))
            .Returns(true);
        
        var cacheEntry = Mock.Of<ICacheEntry>();
        mockMemoryCache
            .Setup(m => m.CreateEntry(It.IsAny<object>()))
            .Returns(cacheEntry);
        
        var request = RadiusPacketFactory.AccessRequest();
        request.AddAttribute("User-Password", "password");
        var client = new ClientConfiguration("cli_config", "rds", AuthenticationSource.None, "key", "secret");
        var context = new RadiusContext(request, client, new Mock<IServiceProvider>().Object)
        {
            RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636),
        };

        context.SetMessageState(passwordChangeRequestId);
        var identifier = new ChallengeIdentifier("test", passwordChangeRequestId);
        var dataProtectionProviderMock = new Mock<IDataProtectionProvider>();
        dataProtectionProviderMock.Setup(x => x.CreateProtector(It.IsAny<string>())).Returns(Mock.Of<IDataProtector>());
        var dataProtectionServiceMock = new DataProtectionService(dataProtectionProviderMock.Object);
        var processor = new ChangePasswordChallengeProcessor(mockMemoryCache.Object, ldapService.Object, dataProtectionServiceMock, new Mock<ILogger<ChangePasswordChallengeProcessor>>().Object);

        var result = await processor.ProcessChallengeAsync(identifier, context);

        Assert.Equal(ChallengeCode.InProcess, result);
        Assert.Equal(passwordChangeRequestId, context.State);
    }

    [Fact]
    public async Task ProcessChallengeAsync_RequestPasswordNotMatched_ShouldReturnInProgressStatusAndUpdateState()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
            });
        });
        
        var ldapService = new Mock<ILdapService>();
        
        var dataProtectionProvider = host.Service<IDataProtectionProvider>();
        var dataProtectionService = new DataProtectionService(dataProtectionProvider);
        
        var encryptedPassword = dataProtectionService.Protect("newPassword", Constants.PasswordProtector);
        
        object passwordChangeRequest = new PasswordChangeRequest() { NewPasswordEncryptedData = encryptedPassword };
        var passwordChangeRequestId = ((PasswordChangeRequest)passwordChangeRequest).Id;
        var mockMemoryCache = new Mock<IMemoryCache>();
        
        mockMemoryCache
            .Setup(mc => mc.TryGetValue(It.IsAny<string>(), out passwordChangeRequest))
            .Returns(true);
        
        var cacheEntry = Mock.Of<ICacheEntry>();
        mockMemoryCache
            .Setup(m => m.CreateEntry(It.IsAny<object>()))
            .Returns(cacheEntry);
        
        var request = RadiusPacketFactory.AccessRequest();
        request.AddAttribute("User-Password", "password");
        
        var client = new ClientConfiguration("cli_config", "rds", AuthenticationSource.None, "key", "secret");
        var context = new RadiusContext(request, client, new Mock<IServiceProvider>().Object)
        {
            RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636),
        };

        context.SetMessageState("oldState");
        var identifier = new ChallengeIdentifier("test", passwordChangeRequestId);
        var processor = new ChangePasswordChallengeProcessor(mockMemoryCache.Object, ldapService.Object, dataProtectionService, host.Service<ILogger<ChangePasswordChallengeProcessor>>());

        var result = await processor.ProcessChallengeAsync(identifier, context);

        Assert.Equal(ChallengeCode.InProcess, result);
        Assert.Equal(passwordChangeRequestId, context.State);
    }
    
    [Fact]
    public async Task ProcessChallengeAsync_PasswordMatchedAndSuccessLdapResponse_ShouldReturnAcceptAndDeleteState()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
            });
        });
        
        var password = "password";
        
        var dataProtectionProvider = host.Service<IDataProtectionProvider>();
        var dataProtectionService = new DataProtectionService(dataProtectionProvider);
        
        var encryptedPassword = dataProtectionService.Protect(password, Constants.PasswordProtector);
        var ldapService = new Mock<ILdapService>();
        
        ldapService.Setup(x => x.ChangeUserPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<RadiusContext>())).ReturnsAsync(new PasswordChangeResponse() { Success = true });
        
        object passwordChangeRequest = new PasswordChangeRequest() { NewPasswordEncryptedData = encryptedPassword, CurrentPasswordEncryptedData = encryptedPassword };
        var passwordChangeRequestId = ((PasswordChangeRequest)passwordChangeRequest).Id;
        var mockMemoryCache = new Mock<IMemoryCache>();
        
        mockMemoryCache
            .Setup(mc => mc.TryGetValue(It.IsAny<string>(), out passwordChangeRequest))
            .Returns(true);
        
        var cacheEntry = Mock.Of<ICacheEntry>();
        mockMemoryCache
            .Setup(m => m.CreateEntry(It.IsAny<object>()))
            .Returns(cacheEntry);
        
        var request = RadiusPacketFactory.AccessRequest();
        request.AddAttribute("User-Password", password);
        
        var client = new ClientConfiguration("cli_config", "rds", AuthenticationSource.None, "key", "secret");
        var context = new RadiusContext(request, client, new Mock<IServiceProvider>().Object)
        {
            RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636),
        };

        context.SetMessageState(passwordChangeRequestId);
        var identifier = new ChallengeIdentifier("test", passwordChangeRequestId);

        var processor = new ChangePasswordChallengeProcessor(mockMemoryCache.Object, ldapService.Object, dataProtectionService, host.Service<ILogger<ChangePasswordChallengeProcessor>>());

        var result = await processor.ProcessChallengeAsync(identifier, context);

        Assert.Equal(ChallengeCode.Accept, result);
        Assert.Null(context.State);
    }
    
    [Fact]
    public async Task ProcessChallengeAsync_PasswordMatchedAndSuccessLdapResponse_ShouldReturnRejectAndDeleteState()
    {
        var host = TestHostFactory.CreateHost(builder =>
        {
            builder.Services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
            });
        });
        
        var password = "password";
        
        var dataProtectionProvider = host.Service<IDataProtectionProvider>();
        var dataProtectionService = new DataProtectionService(dataProtectionProvider);
        
        var encryptedPassword = dataProtectionService.Protect(password, Constants.PasswordProtector);
        var ldapService = new Mock<ILdapService>();
        
        ldapService.Setup(x => x.ChangeUserPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<RadiusContext>())).ReturnsAsync(new PasswordChangeResponse() { Success = false });
        
        object passwordChangeRequest = new PasswordChangeRequest() { NewPasswordEncryptedData = encryptedPassword, CurrentPasswordEncryptedData = encryptedPassword };
        var passwordChangeRequestId = ((PasswordChangeRequest)passwordChangeRequest).Id;
        var mockMemoryCache = new Mock<IMemoryCache>();
        
        mockMemoryCache
            .Setup(mc => mc.TryGetValue(It.IsAny<string>(), out passwordChangeRequest))
            .Returns(true);
        
        var cacheEntry = Mock.Of<ICacheEntry>();
        mockMemoryCache
            .Setup(m => m.CreateEntry(It.IsAny<object>()))
            .Returns(cacheEntry);
        
        var request = RadiusPacketFactory.AccessRequest();
        request.AddAttribute("User-Password", password);
        
        var client = new ClientConfiguration("cli_config", "rds", AuthenticationSource.None, "key", "secret");
        var context = new RadiusContext(request, client, new Mock<IServiceProvider>().Object)
        {
            RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636),
        };

        context.SetMessageState(passwordChangeRequestId);
        var identifier = new ChallengeIdentifier("test", passwordChangeRequestId);
        var processor = new ChangePasswordChallengeProcessor(mockMemoryCache.Object, ldapService.Object, dataProtectionService, new Mock<ILogger<ChangePasswordChallengeProcessor>>().Object);

        var result = await processor.ProcessChallengeAsync(identifier, context);

        Assert.Equal(ChallengeCode.Reject, result);
        Assert.Null(context.State);
    }
}