using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Multifactor.Radius.Adapter.v2.Core;
using Multifactor.Radius.Adapter.v2.Core.AccessChallenge;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.Auth.PreAuthMode;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.Ldap;
using Multifactor.Radius.Adapter.v2.Core.MultifactorApi;
using Multifactor.Radius.Adapter.v2.Core.Pipeline;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;
using Multifactor.Radius.Adapter.v2.Services.DataProtection;
using Multifactor.Radius.Adapter.v2.Services.Ldap;
using Multifactor.Radius.Adapter.v2.Tests.Fixture;

namespace Multifactor.Radius.Adapter.v2.Tests.AccessChallengeTests;

public class ChangePasswordChallengeProcessorTests
{
    [Fact]
    public void ShouldReturnCorrectChallengeType()
    {
        var memCacheMock = new Mock<IMemoryCache>();
        var service = new Mock<ILdapProfileService>();
        var dataProtectionService = new Mock<IDataProtectionService>();
        var processor = new ChangePasswordChallengeProcessor(memCacheMock.Object, service.Object,
            dataProtectionService.Object, NullLogger<ChangePasswordChallengeProcessor>.Instance);
        Assert.Equal(ChallengeType.PasswordChange, processor.ChallengeType);
    }

    [Fact]
    public void AddChallengeContext_NoContext_ShouldThrowArgumentNullException()
    {
        var memCacheMock = new Mock<IMemoryCache>();
        var service = new Mock<ILdapProfileService>();
        var dataProtectionService = new Mock<IDataProtectionService>();
        var processor = new ChangePasswordChallengeProcessor(memCacheMock.Object, service.Object,
            dataProtectionService.Object, NullLogger<ChangePasswordChallengeProcessor>.Instance);

        Assert.Throws<ArgumentNullException>(() => processor.AddChallengeContext(null));
    }

    [Theory]
    [ClassData(typeof(EmptyStringsListInput))]
    public void AddChallengeContext_NoPassword_ShouldThrowArgumentNullException(string emptyString)
    {
        var memCacheMock = new Mock<IMemoryCache>();
        var service = new Mock<ILdapProfileService>();
        var dataProtectionService = new Mock<IDataProtectionService>();
        var processor = new ChangePasswordChallengeProcessor(memCacheMock.Object, service.Object,
            dataProtectionService.Object, NullLogger<ChangePasswordChallengeProcessor>.Instance);

        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var passphrase = UserPassphrase.Parse(emptyString, PreAuthModeDescriptor.Default);
        contextMock.Setup(x => x.Passphrase).Returns(passphrase);
        var context = contextMock.Object;
        Assert.Throws<InvalidOperationException>(() => processor.AddChallengeContext(context));
    }


    [Theory]
    [ClassData(typeof(EmptyStringsListInput))]
    public void AddChallengeContext_NoDomain_ShouldThrowArgumentNullException(string emptyString)
    {
        var memCacheMock = new Mock<IMemoryCache>();
        var service = new Mock<ILdapProfileService>();
        var dataProtectionService = new Mock<IDataProtectionService>();
        var processor = new ChangePasswordChallengeProcessor(memCacheMock.Object, service.Object,
            dataProtectionService.Object, NullLogger<ChangePasswordChallengeProcessor>.Instance);

        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var passphrase = UserPassphrase.Parse("123456", PreAuthModeDescriptor.Default);
        contextMock.Setup(x => x.Passphrase).Returns(passphrase);
        contextMock.Setup(x => x.MustChangePasswordDomain).Returns(emptyString);
        var context = contextMock.Object;
        Assert.Throws<InvalidOperationException>(() => processor.AddChallengeContext(context));
    }

    [Fact]
    public void AddChallengeContext_ShouldAdd()
    {
        var service = new Mock<ILdapProfileService>();
        var dataProtectionServiceMock = new Mock<IDataProtectionService>();
        dataProtectionServiceMock.Setup(x => x.Protect(It.IsAny<string>(), It.IsAny<string>())).Returns("password");
        var dataProtectionService = dataProtectionServiceMock.Object;
        var processor = new ChangePasswordChallengeProcessor(new CacheMock(), service.Object, dataProtectionService,
            NullLogger<ChangePasswordChallengeProcessor>.Instance);
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var passphrase = UserPassphrase.Parse("123456", PreAuthModeDescriptor.Default);
        contextMock.Setup(x => x.Passphrase).Returns(passphrase);
        contextMock.Setup(x => x.Settings.ClientConfigurationName).Returns("name");
        contextMock.Setup(x => x.MustChangePasswordDomain).Returns("domain");
        contextMock.Setup(x => x.Settings.ApiCredential).Returns(new ApiCredential("key", "secret"));
        contextMock.SetupProperty(x => x.ResponseInformation);
        var context = contextMock.Object;
        context.ResponseInformation = new ResponseInformation();

        var id = processor.AddChallengeContext(context);

        Assert.NotNull(id);
        Assert.NotNull(context.ResponseInformation.State);
        Assert.NotNull(context.ResponseInformation.ReplyMessage);
        Assert.NotEmpty(context.ResponseInformation.State);
        Assert.NotEmpty(context.ResponseInformation.ReplyMessage);
    }

    [Fact]
    public async Task ProcessChallenge_EmptyContext_ShouldThrowArgumentNullException()
    {
        var service = new Mock<ILdapProfileService>();
        var dataProtectionServiceMock = new Mock<IDataProtectionService>();
        dataProtectionServiceMock.Setup(x => x.Protect(It.IsAny<string>(), It.IsAny<string>())).Returns("password");
        var dataProtectionService = dataProtectionServiceMock.Object;
        var processor = new ChangePasswordChallengeProcessor(new CacheMock(), service.Object, dataProtectionService, NullLogger<ChangePasswordChallengeProcessor>.Instance);
        var id = new ChallengeIdentifier("1", "2");
        
        await Assert.ThrowsAsync<ArgumentNullException>(() => processor.ProcessChallengeAsync(id, null));
    }

    [Fact]
    public async Task ProcessChallenge_NoRequest_ShouldReturnAccept()
    {
        var service = new Mock<ILdapProfileService>();
        var dataProtectionServiceMock = new Mock<IDataProtectionService>();
        dataProtectionServiceMock.Setup(x => x.Protect(It.IsAny<string>(), It.IsAny<string>())).Returns("password");
        var dataProtectionService = dataProtectionServiceMock.Object;
        var processor = new ChangePasswordChallengeProcessor(new CacheMock(), service.Object, dataProtectionService,
            NullLogger<ChangePasswordChallengeProcessor>.Instance);
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var passphrase = UserPassphrase.Parse("123456", PreAuthModeDescriptor.Default);
        contextMock.Setup(x => x.Passphrase).Returns(passphrase);
        contextMock.Setup(x => x.Settings.ClientConfigurationName).Returns("name");
        contextMock.Setup(x => x.MustChangePasswordDomain).Returns("domain");
        contextMock.SetupProperty(x => x.ResponseInformation);
        var context = contextMock.Object;
        context.ResponseInformation = new ResponseInformation();
        var request = new ChallengeIdentifier("1", "2");
        
        var status = await processor.ProcessChallengeAsync(request, context);
        
        Assert.Equal(ChallengeStatus.Accept, status);
    }

    [Fact]
    public async Task ProcessChallenge_NoPassword_ShouldReturnReject()
    {
        var service = new Mock<ILdapProfileService>();
        var dataProtectionServiceMock = new Mock<IDataProtectionService>();
        dataProtectionServiceMock.Setup(x => x.Protect(It.IsAny<string>(), It.IsAny<string>())).Returns("password");
        var dataProtectionService = dataProtectionServiceMock.Object;
        var processor = new ChangePasswordChallengeProcessor(new CacheMock(new PasswordChangeRequest()), service.Object,
            dataProtectionService, NullLogger<ChangePasswordChallengeProcessor>.Instance);
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var passphrase = UserPassphrase.Parse(null, PreAuthModeDescriptor.Default);
        contextMock.Setup(x => x.Passphrase).Returns(passphrase);
        contextMock.Setup(x => x.Settings.ClientConfigurationName).Returns("name");
        contextMock.Setup(x => x.MustChangePasswordDomain).Returns("domain");
        contextMock.SetupProperty(x => x.ResponseInformation);
        contextMock.SetupProperty(x => x.AuthenticationState);
        var context = contextMock.Object;
        context.AuthenticationState = new AuthenticationState();
        context.ResponseInformation = new ResponseInformation();
        var request = new ChallengeIdentifier("1", "2");

        var status = await processor.ProcessChallengeAsync(request, context);

        Assert.Equal(ChallengeStatus.Reject, status);
        Assert.Equal(AuthenticationStatus.Reject, context.AuthenticationState.FirstFactorStatus);
    }

    [Fact]
    public async Task ProcessChallenge_NoNewPassword_ShouldReturnInProcess()
    {
        var service = new Mock<ILdapProfileService>();
        var dataProtectionServiceMock = new Mock<IDataProtectionService>();
        dataProtectionServiceMock.Setup(x => x.Protect(It.IsAny<string>(), It.IsAny<string>())).Returns("password");
        var dataProtectionService = dataProtectionServiceMock.Object;
        var processor = new ChangePasswordChallengeProcessor(
            new CacheMock(new PasswordChangeRequest()), service.Object,
            dataProtectionService, NullLogger<ChangePasswordChallengeProcessor>.Instance);
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var passphrase = UserPassphrase.Parse("123456", PreAuthModeDescriptor.Default);
        contextMock.Setup(x => x.Passphrase).Returns(passphrase);
        contextMock.Setup(x => x.Settings.ClientConfigurationName).Returns("name");
        contextMock.Setup(x => x.MustChangePasswordDomain).Returns("domain");
        contextMock.Setup(x => x.Settings.ApiCredential).Returns(new ApiCredential("key", "secret"));
        contextMock.SetupProperty(x => x.ResponseInformation);
        contextMock.SetupProperty(x => x.AuthenticationState);
        var context = contextMock.Object;
        context.AuthenticationState = new AuthenticationState();
        context.ResponseInformation = new ResponseInformation();
        var request = new ChallengeIdentifier("1", "2");

        var status = await processor.ProcessChallengeAsync(request, context);

        Assert.Equal(ChallengeStatus.InProcess, status);
        Assert.Equal(AuthenticationStatus.Awaiting, context.AuthenticationState.FirstFactorStatus);
    }

    [Fact]
    public async Task ProcessChallenge_NotMatchChallenge_ShouldReturnInProcess()
    {
        var service = new Mock<ILdapProfileService>();
        var dataProtectionServiceMock = new Mock<IDataProtectionService>();
        dataProtectionServiceMock.Setup(x => x.Protect(It.IsAny<string>(), It.IsAny<string>())).Returns("password");
        var dataProtectionService = dataProtectionServiceMock.Object;
        var processor = new ChangePasswordChallengeProcessor(
            new CacheMock(new PasswordChangeRequest() { NewPasswordEncryptedData = "password" }), service.Object,
            dataProtectionService, NullLogger<ChangePasswordChallengeProcessor>.Instance);
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var passphrase = UserPassphrase.Parse("123456", PreAuthModeDescriptor.Default);
        contextMock.Setup(x => x.Passphrase).Returns(passphrase);
        contextMock.Setup(x => x.Settings.ClientConfigurationName).Returns("name");
        contextMock.Setup(x => x.MustChangePasswordDomain).Returns("domain");
        contextMock.Setup(x => x.Settings.ApiCredential).Returns(new ApiCredential("key", "secret"));
        contextMock.SetupProperty(x => x.ResponseInformation);
        contextMock.SetupProperty(x => x.AuthenticationState);
        var context = contextMock.Object;
        context.AuthenticationState = new AuthenticationState();
        context.ResponseInformation = new ResponseInformation();
        var request = new ChallengeIdentifier("1", "2");

        var status = await processor.ProcessChallengeAsync(request, context);

        Assert.Equal(ChallengeStatus.InProcess, status);
        Assert.Equal(AuthenticationStatus.Awaiting, context.AuthenticationState.FirstFactorStatus);
        Assert.StartsWith("Passwords not match", context.ResponseInformation.ReplyMessage);
    }

    [Fact]
    public async Task ProcessChallenge_SuccessfulPasswordChange_ShouldReturnAccept()
    {
        var service = new Mock<ILdapProfileService>();
        service
            .Setup(x => x.ChangeUserPasswordAsync(It.IsAny<string>(), It.IsAny<ILdapProfile>(),
                It.IsAny<ILdapServerConfiguration>()))
            .ReturnsAsync(() => new PasswordChangeResponse() { Success = true });

        var dataProtectionServiceMock = new Mock<IDataProtectionService>();
        dataProtectionServiceMock.Setup(x => x.Protect(It.IsAny<string>(), It.IsAny<string>())).Returns("password");
        dataProtectionServiceMock.Setup(x => x.Unprotect(It.IsAny<string>(), It.IsAny<string>())).Returns("1234567");
        var dataProtectionService = dataProtectionServiceMock.Object;
        var processor = new ChangePasswordChallengeProcessor(
            new CacheMock(new PasswordChangeRequest() { NewPasswordEncryptedData = "1234567" }), service.Object,
            dataProtectionService, NullLogger<ChangePasswordChallengeProcessor>.Instance);
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var passphrase = UserPassphrase.Parse("1234567", PreAuthModeDescriptor.Default);
        contextMock.Setup(x => x.Passphrase).Returns(passphrase);
        contextMock.Setup(x => x.Settings.ClientConfigurationName).Returns("name");
        contextMock.Setup(x => x.MustChangePasswordDomain).Returns("domain");
        contextMock.Setup(x => x.Settings.ApiCredential).Returns(new ApiCredential("key", "secret"));
        contextMock.SetupProperty(x => x.ResponseInformation);
        contextMock.SetupProperty(x => x.AuthenticationState);
        var context = contextMock.Object;
        context.AuthenticationState = new AuthenticationState();
        context.ResponseInformation = new ResponseInformation();
        var request = new ChallengeIdentifier("1", "2");

        var status = await processor.ProcessChallengeAsync(request, context);

        Assert.Equal(ChallengeStatus.Accept, status);
        Assert.Equal(AuthenticationStatus.Awaiting, context.AuthenticationState.FirstFactorStatus);
        Assert.Null(context.ResponseInformation.State);
    }

    [Fact]
    public async Task ProcessChallenge_UnsuccessfulPasswordChange_ShouldReturnAccept()
    {
        var service = new Mock<ILdapProfileService>();
        service
            .Setup(x => x.ChangeUserPasswordAsync(It.IsAny<string>(), It.IsAny<ILdapProfile>(),
                It.IsAny<ILdapServerConfiguration>()))
            .ReturnsAsync(() => new PasswordChangeResponse() { Success = false });

        var dataProtectionServiceMock = new Mock<IDataProtectionService>();
        dataProtectionServiceMock.Setup(x => x.Protect(It.IsAny<string>(), It.IsAny<string>())).Returns("password");
        dataProtectionServiceMock.Setup(x => x.Unprotect(It.IsAny<string>(), It.IsAny<string>())).Returns("1234567");
        var dataProtectionService = dataProtectionServiceMock.Object;
        var processor = new ChangePasswordChallengeProcessor(
            new CacheMock(new PasswordChangeRequest() { NewPasswordEncryptedData = "1234567" }), service.Object,
            dataProtectionService, NullLogger<ChangePasswordChallengeProcessor>.Instance);
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var passphrase = UserPassphrase.Parse("1234567", PreAuthModeDescriptor.Default);
        contextMock.Setup(x => x.Passphrase).Returns(passphrase);
        contextMock.Setup(x => x.Settings.ClientConfigurationName).Returns("name");
        contextMock.Setup(x => x.MustChangePasswordDomain).Returns("domain");
        contextMock.Setup(x => x.Settings.ApiCredential).Returns(new ApiCredential("key", "secret"));
        contextMock.SetupProperty(x => x.ResponseInformation);
        contextMock.SetupProperty(x => x.AuthenticationState);
        var context = contextMock.Object;
        context.AuthenticationState = new AuthenticationState();
        context.ResponseInformation = new ResponseInformation();
        var request = new ChallengeIdentifier("1", "2");

        var status = await processor.ProcessChallengeAsync(request, context);

        Assert.Equal(ChallengeStatus.Reject, status);
        Assert.Equal(AuthenticationStatus.Reject, context.AuthenticationState.FirstFactorStatus);
        Assert.Null(context.ResponseInformation.State);
    }
    
    private class CacheMock() : IMemoryCache
    {
        private object? _val;

        public CacheMock(object? val = null) : this()
        {
            _val = val;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue(object key, out object? value)
        {
            value = _val;
            return value is null;
        }

        public ICacheEntry CreateEntry(object key)
        {
            var entry = new Mock<ICacheEntry>();
            entry.SetupProperty(x => x.AbsoluteExpiration);
            entry.SetupProperty(x => x.Value);
            return entry.Object;
        }

        public void Remove(object key)
        {
        }
    }
}