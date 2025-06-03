using System.DirectoryServices.Protocols;
using System.Net;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Connection.LdapConnectionFactory;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.Auth.PreAuthMode;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.FirstFactor;
using Multifactor.Radius.Adapter.v2.Core.Radius.Packet;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;
using Multifactor.Radius.Adapter.v2.Tests.Fixture;

namespace Multifactor.Radius.Adapter.v2.Tests.FirstFactorAuthTests;

public class LdapFirstFactorProcessorTests
{
    [Fact]
    public async Task LdapFirstFactorProcessor_CorrectCredentials_ShouldAccept()
    {
        var sensitiveData = GetConfig();
        var processor = new LdapFirstFactorProcessor(LdapConnectionFactory.Create(),
            NullLogger<LdapFirstFactorProcessor>.Instance);

        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var packetMock = new Mock<IRadiusPacket>();
        packetMock.Setup(x => x.UserName).Returns(sensitiveData["UserName"]);
        packetMock.Setup(x => x.TryGetUserPassword()).Returns(sensitiveData["Password"]);
        contextMock.Setup(x => x.RequestPacket).Returns(packetMock.Object);

        var serverSettings = new Mock<ILdapServerConfiguration>();
        serverSettings.Setup(x => x.ConnectionString).Returns(sensitiveData["ConnectionString"]);
        serverSettings.Setup(x => x.BindTimeoutInSeconds).Returns(30);
        contextMock.Setup(x => x.Settings.LdapServers).Returns(new[] { serverSettings.Object });

        var authState = new AuthenticationState();
        contextMock.Setup(x => x.AuthenticationState).Returns(authState);

        var transformRules = new UserNameTransformRules();
        contextMock.Setup(x => x.Settings.UserNameTransformRules).Returns(transformRules);

        contextMock.Setup(x => x.Settings.PreAuthnMode).Returns(PreAuthModeDescriptor.Default);

        await processor.ProcessFirstFactor(contextMock.Object);
        Assert.Equal(AuthenticationStatus.Accept, authState.FirstFactorStatus);
    }
    
    [Fact]
    public async Task LdapFirstFactorProcessor_CorrectCredentials_ShouldSaveServerConfiguration()
    {
        var sensitiveData = GetConfig();
        var processor = new LdapFirstFactorProcessor(LdapConnectionFactory.Create(),
            NullLogger<LdapFirstFactorProcessor>.Instance);

        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.SetupProperty(x => x.FirstFactorLdapServerConfiguration);
        var packetMock = new Mock<IRadiusPacket>();
        packetMock.Setup(x => x.UserName).Returns(sensitiveData["UserName"]);
        packetMock.Setup(x => x.TryGetUserPassword()).Returns(sensitiveData["Password"]);
        contextMock.Setup(x => x.RequestPacket).Returns(packetMock.Object);

        var serverSettings = new Mock<ILdapServerConfiguration>();
        serverSettings.Setup(x => x.ConnectionString).Returns(sensitiveData["ConnectionString"]);
        serverSettings.Setup(x => x.BindTimeoutInSeconds).Returns(30);
        contextMock.Setup(x => x.Settings.LdapServers).Returns(new[] { serverSettings.Object });

        var authState = new AuthenticationState();
        contextMock.Setup(x => x.AuthenticationState).Returns(authState);

        var transformRules = new UserNameTransformRules();
        contextMock.Setup(x => x.Settings.UserNameTransformRules).Returns(transformRules);

        contextMock.Setup(x => x.Settings.PreAuthnMode).Returns(PreAuthModeDescriptor.Default);
        var context = contextMock.Object;
        await processor.ProcessFirstFactor(context);
        Assert.NotNull(context.FirstFactorLdapServerConfiguration);
        Assert.Equal(sensitiveData["ConnectionString"], context.FirstFactorLdapServerConfiguration.ConnectionString);
    }

    [Fact]
    public async Task LdapFirstFactorProcessor_IncorrectPassword_ShouldReject()
    {
        var sensitiveData = GetConfig();
        var processor = new LdapFirstFactorProcessor(LdapConnectionFactory.Create(),
            NullLogger<LdapFirstFactorProcessor>.Instance);
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
        contextMock.Setup(x => x.Settings.LdapServers).Returns(new[] { serverSettings.Object });
        contextMock.Setup(x => x.AuthenticationState).Returns(authState);
        contextMock.Setup(x => x.Settings.UserNameTransformRules).Returns(transformRules);
        contextMock.Setup(x => x.Settings.PreAuthnMode).Returns(PreAuthModeDescriptor.Default);

        await processor.ProcessFirstFactor(contextMock.Object);
        Assert.Equal(AuthenticationStatus.Reject, authState.FirstFactorStatus);
    }
    
    [Fact]
    public async Task LdapFirstFactorProcessor_IncorrectPassword_ShouldNotSaveLdapServerConfiguration()
    {
        var sensitiveData = GetConfig();
        var processor = new LdapFirstFactorProcessor(LdapConnectionFactory.Create(),
            NullLogger<LdapFirstFactorProcessor>.Instance);
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
        contextMock.Setup(x => x.Settings.LdapServers).Returns(new[] { serverSettings.Object });
        contextMock.Setup(x => x.AuthenticationState).Returns(authState);
        contextMock.Setup(x => x.Settings.UserNameTransformRules).Returns(transformRules);
        contextMock.Setup(x => x.Settings.PreAuthnMode).Returns(PreAuthModeDescriptor.Default);
        contextMock.SetupProperty(x => x.FirstFactorLdapServerConfiguration);
        var context = contextMock.Object;
        await processor.ProcessFirstFactor(context);
        Assert.Null(context.FirstFactorLdapServerConfiguration);
    }

    [Fact]
    public async Task LdapFirstFactorProcessor_IncorrectLogin_ShouldReject()
    {
        var sensitiveData = GetConfig();
        var processor = new LdapFirstFactorProcessor(LdapConnectionFactory.Create(),
            NullLogger<LdapFirstFactorProcessor>.Instance);
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
        contextMock.Setup(x => x.Settings.LdapServers).Returns(new[] { serverSettings.Object });
        contextMock.Setup(x => x.AuthenticationState).Returns(authState);
        contextMock.Setup(x => x.Settings.UserNameTransformRules).Returns(transformRules);
        contextMock.Setup(x => x.Settings.PreAuthnMode).Returns(PreAuthModeDescriptor.Default);

        await processor.ProcessFirstFactor(contextMock.Object);
        Assert.Equal(AuthenticationStatus.Reject, authState.FirstFactorStatus);
    }

    [Theory]
    [ClassData(typeof(EmptyStringsListInput))]
    public async Task LdapFirstFactorProcessor_EmptyLogin_ShouldReject(string login)
    {
        var processor = new LdapFirstFactorProcessor(LdapConnectionFactory.Create(),
            NullLogger<LdapFirstFactorProcessor>.Instance);
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var packetMock = new Mock<IRadiusPacket>();
        var authState = new AuthenticationState();
        var transformRules = new UserNameTransformRules();
        packetMock.Setup(x => x.UserName).Returns(login);
        packetMock.Setup(x => x.TryGetUserPassword()).Returns("correctLogin");
        packetMock.Setup(x => x.Identifier).Returns(0);
        contextMock.Setup(x => x.RequestPacket).Returns(packetMock.Object);
        contextMock.Setup(x => x.RemoteEndpoint).Returns(IPEndPoint.Parse("127.0.0.1:8080"));
        contextMock.Setup(x => x.Settings.LdapServers).Returns(new[] { new Mock<ILdapServerConfiguration>().Object });
        contextMock.Setup(x => x.AuthenticationState).Returns(authState);
        contextMock.Setup(x => x.Settings.UserNameTransformRules).Returns(transformRules);
        contextMock.Setup(x => x.Settings.PreAuthnMode).Returns(PreAuthModeDescriptor.Default);

        await processor.ProcessFirstFactor(contextMock.Object);
        Assert.Equal(AuthenticationStatus.Reject, authState.FirstFactorStatus);
    }

    [Theory]
    [ClassData(typeof(EmptyStringsListInput))]
    public async Task LdapFirstFactorProcessor_EmptyPassword_ShouldReject(string pwd)
    {
        var processor = new LdapFirstFactorProcessor(LdapConnectionFactory.Create(),
            NullLogger<LdapFirstFactorProcessor>.Instance);
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var packetMock = new Mock<IRadiusPacket>();
        var authState = new AuthenticationState();
        var transformRules = new UserNameTransformRules();
        packetMock.Setup(x => x.UserName).Returns("correctLogin");
        packetMock.Setup(x => x.TryGetUserPassword()).Returns(pwd);
        packetMock.Setup(x => x.Identifier).Returns(0);
        contextMock.Setup(x => x.RequestPacket).Returns(packetMock.Object);
        contextMock.Setup(x => x.RemoteEndpoint).Returns(IPEndPoint.Parse("127.0.0.1:8080"));
        contextMock.Setup(x => x.Settings.LdapServers).Returns(new[] { new Mock<ILdapServerConfiguration>().Object });
        contextMock.Setup(x => x.AuthenticationState).Returns(authState);
        contextMock.Setup(x => x.Settings.UserNameTransformRules).Returns(transformRules);
        contextMock.Setup(x => x.Settings.PreAuthnMode).Returns(PreAuthModeDescriptor.Default);

        await processor.ProcessFirstFactor(contextMock.Object);
        Assert.Equal(AuthenticationStatus.Reject, authState.FirstFactorStatus);
    }

    [Fact]
    public async Task LdapFirstFactorProcessor_MustChangePasswordResponse_ShouldReject()
    {
        var factoryMock = new Mock<ILdapConnectionFactory>();
        factoryMock.Setup(x => x.CreateConnection(It.IsAny<LdapConnectionOptions>())).Throws(GetLdapException);
        factoryMock.Setup(x => x.TargetPlatform).Returns(OSPlatform.Windows);
        var factory = new LdapConnectionFactory(NullLogger<LdapConnectionFactory>.Instance, [factoryMock.Object]);
        var processor = new LdapFirstFactorProcessor(factory, NullLogger<LdapFirstFactorProcessor>.Instance);
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var packetMock = new Mock<IRadiusPacket>();
        var serverSettings = new Mock<ILdapServerConfiguration>();
        var authState = new AuthenticationState();
        var transformRules = new UserNameTransformRules();
        packetMock.Setup(x => x.UserName).Returns("user");
        packetMock.Setup(x => x.TryGetUserPassword()).Returns("pwd");
        contextMock.Setup(x => x.RequestPacket).Returns(packetMock.Object);
        serverSettings.Setup(x => x.ConnectionString).Returns("your.domain");
        serverSettings.Setup(x => x.BindTimeoutInSeconds).Returns(30);
        contextMock.Setup(x => x.Settings.LdapServers).Returns(new[] { serverSettings.Object });
        contextMock.Setup(x => x.AuthenticationState).Returns(authState);
        contextMock.Setup(x => x.Settings.UserNameTransformRules).Returns(transformRules);
        contextMock.Setup(x => x.Settings.PreAuthnMode).Returns(PreAuthModeDescriptor.Default);
        contextMock.SetupProperty(x => x.MustChangePasswordDomain);
        var context = contextMock.Object;

        await processor.ProcessFirstFactor(context);
        Assert.Equal(AuthenticationStatus.Reject, authState.FirstFactorStatus);
        Assert.Equal("your.domain", context.MustChangePasswordDomain);
    }

    private LdapException GetLdapException()
    {
        var ex = new LdapException("data 773");
        return ex;
    }

    private Dictionary<string, string> GetConfig()
    {
        return ConfigUtils.GetConfigSensitiveData("LdapFirstFactorProcessorTests.txt", "|");
    }
}