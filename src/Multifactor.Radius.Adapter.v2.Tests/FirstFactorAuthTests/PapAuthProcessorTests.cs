using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Core;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.Auth.PreAuthMode;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.FirstFactor.BindNameFormat;
using Multifactor.Radius.Adapter.v2.Core.FirstFactor.LdapAuth;
using Multifactor.Radius.Adapter.v2.Core.Ldap;
using Multifactor.Radius.Adapter.v2.Core.Radius.Packet;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;
using Multifactor.Radius.Adapter.v2.Tests.Fixture;

namespace Multifactor.Radius.Adapter.v2.Tests.FirstFactorAuthTests;

[Collection("LDAP")]
public class PapAuthProcessorTests
{
    [Fact]
    public async Task LdapFirstFactorProcessor_CorrectCredentials_ShouldAccept()
    {
        var sensitiveData = GetConfig();
        var processor = new PapAuthProcessor(new CustomLdapConnectionFactory(), new Mock<ILdapBindNameFormatterProvider>().Object, NullLogger<PapAuthProcessor>.Instance);

        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var packetMock = new Mock<IRadiusPacket>();
        packetMock.Setup(x => x.UserName).Returns(sensitiveData["UserName"]);
        packetMock.Setup(x => x.TryGetUserPassword()).Returns(sensitiveData["Password"]);
        contextMock.Setup(x => x.RequestPacket).Returns(packetMock.Object);

        var serverSettings = new Mock<ILdapServerConfiguration>();
        serverSettings.Setup(x => x.ConnectionString).Returns(sensitiveData["ConnectionString"]);
        serverSettings.Setup(x => x.BindTimeoutInSeconds).Returns(30);
        contextMock.Setup(x => x.LdapServerConfiguration).Returns(serverSettings.Object);
        contextMock.Setup(x => x.LdapSchema).Returns(new Mock<ILdapSchema>().Object);
        
        var authState = new AuthenticationState();
        contextMock.Setup(x => x.AuthenticationState).Returns(authState);

        var transformRules = new UserNameTransformRules();
        contextMock.Setup(x => x.UserNameTransformRules).Returns(transformRules);
        contextMock.Setup(x => x.PreAuthnMode).Returns(PreAuthModeDescriptor.Default);
        contextMock.Setup(x => x.Passphrase).Returns(UserPassphrase.Parse(sensitiveData["Password"], PreAuthModeDescriptor.Default));
        var result = await processor.Auth(contextMock.Object);
        Assert.True(result.IsSuccess);
    }
    
    [Fact]
    public async Task LdapFirstFactorProcessor_IncorrectPassword_ShouldReject()
    {
        var sensitiveData = GetConfig();
        var processor = new PapAuthProcessor(new CustomLdapConnectionFactory(), new Mock<ILdapBindNameFormatterProvider>().Object, NullLogger<PapAuthProcessor>.Instance);
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var packetMock = new Mock<IRadiusPacket>();
        var serverSettings = new Mock<ILdapServerConfiguration>();
        var authState = new AuthenticationState();
        var transformRules = new UserNameTransformRules();
        packetMock.Setup(x => x.UserName).Returns(sensitiveData["UserName"]);
        packetMock.Setup(x => x.TryGetUserPassword()).Returns("pwd");
        contextMock.Setup(x => x.RequestPacket).Returns(packetMock.Object);
        serverSettings.Setup(x => x.ConnectionString).Returns(sensitiveData["ConnectionString"]);
        serverSettings.Setup(x => x.BindTimeoutInSeconds).Returns(30);
        contextMock.Setup(x => x.LdapServerConfiguration).Returns(serverSettings.Object);
        contextMock.Setup(x => x.AuthenticationState).Returns(authState);
        contextMock.Setup(x => x.UserNameTransformRules).Returns(transformRules);
        contextMock.Setup(x => x.PreAuthnMode).Returns(PreAuthModeDescriptor.Default);
        contextMock.Setup(x => x.Passphrase).Returns(UserPassphrase.Parse("123456", PreAuthModeDescriptor.Default));
        contextMock.Setup(x => x.LdapSchema).Returns(new Mock<ILdapSchema>().Object);
        
        var result = await processor.Auth(contextMock.Object);
        Assert.False(result.IsSuccess);
    }
    
    [Fact]
    public async Task LdapFirstFactorProcessor_IncorrectLogin_ShouldReject()
    {
        var sensitiveData = GetConfig();
        var processor = new PapAuthProcessor(new CustomLdapConnectionFactory(), new Mock<ILdapBindNameFormatterProvider>().Object, NullLogger<PapAuthProcessor>.Instance);
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var packetMock = new Mock<IRadiusPacket>();
        var serverSettings = new Mock<ILdapServerConfiguration>();
        var authState = new AuthenticationState();
        var transformRules = new UserNameTransformRules();
        packetMock.Setup(x => x.UserName).Returns("userName");
        packetMock.Setup(x => x.TryGetUserPassword()).Returns(sensitiveData["Password"]);
        contextMock.Setup(x => x.RequestPacket).Returns(packetMock.Object);
        serverSettings.Setup(x => x.ConnectionString).Returns(sensitiveData["ConnectionString"]);
        serverSettings.Setup(x => x.BindTimeoutInSeconds).Returns(30);
        contextMock.Setup(x => x.LdapServerConfiguration).Returns(serverSettings.Object);
        contextMock.Setup(x => x.AuthenticationState).Returns(authState);
        contextMock.Setup(x => x.UserNameTransformRules).Returns(transformRules);
        contextMock.Setup(x => x.PreAuthnMode).Returns(PreAuthModeDescriptor.Default);
        contextMock.Setup(x => x.Passphrase).Returns(UserPassphrase.Parse("123456", PreAuthModeDescriptor.Default));
        contextMock.Setup(x => x.LdapSchema).Returns(new Mock<ILdapSchema>().Object);
        
        var result = await processor.Auth(contextMock.Object);
        Assert.False(result.IsSuccess);
    }
    
    private Dictionary<string, string> GetConfig()
    {
        return ConfigUtils.GetConfigSensitiveData("LdapFirstFactorProcessorTests.txt", "|");
    }
}