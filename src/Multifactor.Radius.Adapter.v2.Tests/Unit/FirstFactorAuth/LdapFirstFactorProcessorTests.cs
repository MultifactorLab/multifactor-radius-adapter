using System.DirectoryServices.Protocols;
using System.Net;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Core;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.Auth.PreAuthMode;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.FirstFactor;
using Multifactor.Radius.Adapter.v2.Core.FirstFactor.BindNameFormat;
using Multifactor.Radius.Adapter.v2.Core.Ldap;
using Multifactor.Radius.Adapter.v2.Core.Radius.Packet;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;
using Multifactor.Radius.Adapter.v2.Tests.Fixture;
using ILdapConnectionFactory = Multifactor.Core.Ldap.Connection.LdapConnectionFactory.ILdapConnectionFactory;

namespace Multifactor.Radius.Adapter.v2.Tests.Unit.FirstFactorAuth;

public class LdapFirstFactorProcessorTests
{
    [Theory]
    [ClassData(typeof(EmptyStringsListInput))]
    public async Task LdapFirstFactorProcessor_EmptyLogin_ShouldReject(string login)
    {
        //Arrange
        var formatterProviderMock = new LdapBindNameFormatterProvider([]);
        var processor = new LdapFirstFactorProcessor(new CustomLdapConnectionFactory(), formatterProviderMock, NullLogger<LdapFirstFactorProcessor>.Instance);
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var packetMock = new Mock<IRadiusPacket>();
        var authState = new AuthenticationState();
        var transformRules = new UserNameTransformRules();
        packetMock.Setup(x => x.UserName).Returns(login);
        packetMock.Setup(x => x.TryGetUserPassword()).Returns("correctLogin");
        packetMock.Setup(x => x.Identifier).Returns(0);
        contextMock.Setup(x => x.RequestPacket).Returns(packetMock.Object);
        contextMock.Setup(x => x.RemoteEndpoint).Returns(IPEndPoint.Parse("127.0.0.1:8080"));
        contextMock.Setup(x => x.LdapServerConfiguration).Returns(new Mock<ILdapServerConfiguration>().Object);
        contextMock.Setup(x => x.AuthenticationState).Returns(authState);
        contextMock.Setup(x => x.UserNameTransformRules).Returns(transformRules);
        contextMock.Setup(x => x.PreAuthnMode).Returns(PreAuthModeDescriptor.Default);

        //Act
        await processor.ProcessFirstFactor(contextMock.Object);
        
        //Assert
        Assert.Equal(AuthenticationStatus.Reject, authState.FirstFactorStatus);
    }

    [Theory]
    [ClassData(typeof(EmptyStringsListInput))]
    public async Task LdapFirstFactorProcessor_EmptyPassword_ShouldReject(string pwd)
    {
        //Arrange
        var formatterProviderMock = new LdapBindNameFormatterProvider([]);
        var processor = new LdapFirstFactorProcessor(new CustomLdapConnectionFactory(), formatterProviderMock, NullLogger<LdapFirstFactorProcessor>.Instance);
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var packetMock = new Mock<IRadiusPacket>();
        var authState = new AuthenticationState();
        var transformRules = new UserNameTransformRules();
        packetMock.Setup(x => x.UserName).Returns("correctLogin");
        packetMock.Setup(x => x.TryGetUserPassword()).Returns(pwd);
        packetMock.Setup(x => x.Identifier).Returns(0);
        contextMock.Setup(x => x.RequestPacket).Returns(packetMock.Object);
        contextMock.Setup(x => x.RemoteEndpoint).Returns(IPEndPoint.Parse("127.0.0.1:8080"));
        contextMock.Setup(x => x.LdapServerConfiguration).Returns(new Mock<ILdapServerConfiguration>().Object);
        contextMock.Setup(x => x.AuthenticationState).Returns(authState);
        contextMock.Setup(x => x.UserNameTransformRules).Returns(transformRules);
        contextMock.Setup(x => x.PreAuthnMode).Returns(PreAuthModeDescriptor.Default);
        contextMock.Setup(x => x.Passphrase).Returns(UserPassphrase.Parse("123456", PreAuthModeDescriptor.Default));
        
        //Act
        await processor.ProcessFirstFactor(contextMock.Object);
        
        //Assert
        Assert.Equal(AuthenticationStatus.Reject, authState.FirstFactorStatus);
    }

    [Fact]
    public async Task LdapFirstFactorProcessor_MustChangePasswordResponse_ShouldReject()
    {
        //Arrange
        var factoryMock = new Mock<ILdapConnectionFactory>();
        factoryMock.Setup(x => x.CreateConnection(It.IsAny<LdapConnectionOptions>())).Throws(GetLdapException);
        factoryMock.Setup(x => x.TargetPlatform).Returns(OSPlatform.Windows);
        var factory = new CustomLdapConnectionFactory([factoryMock.Object]);
        var formatterProviderMock = new Mock<ILdapBindNameFormatterProvider>();
        var processor = new LdapFirstFactorProcessor(factory, formatterProviderMock.Object, NullLogger<LdapFirstFactorProcessor>.Instance);
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
        contextMock.Setup(x => x.LdapServerConfiguration).Returns(serverSettings.Object);
        contextMock.Setup(x => x.AuthenticationState).Returns(authState);
        contextMock.Setup(x => x.UserNameTransformRules).Returns(transformRules);
        contextMock.Setup(x => x.PreAuthnMode).Returns(PreAuthModeDescriptor.Default);
        contextMock.SetupProperty(x => x.MustChangePasswordDomain);
        contextMock.Setup(x => x.Passphrase).Returns(UserPassphrase.Parse("123456", PreAuthModeDescriptor.Default));
        contextMock.Setup(x => x.LdapSchema.LdapServerImplementation).Returns(LdapImplementation.ActiveDirectory);
        var context = contextMock.Object;

        //Act
        await processor.ProcessFirstFactor(context);
        
        //Assert
        Assert.Equal(AuthenticationStatus.Reject, authState.FirstFactorStatus);
        Assert.Equal("your.domain", context.MustChangePasswordDomain);
    }

    private LdapException GetLdapException()
    {
        var ex = new LdapException(1, "message", "data 773");
        return ex;
    }
}