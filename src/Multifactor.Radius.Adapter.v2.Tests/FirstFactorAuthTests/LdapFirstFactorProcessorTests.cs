using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Multifactor.Core.Ldap.Name;
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


namespace Multifactor.Radius.Adapter.v2.Tests.FirstFactorAuthTests;

[Collection("LDAP")]
public class LdapFirstFactorProcessorTests
{
    [Theory]
    [InlineData("ActiveDirectoryCredentials.txt", "|", LdapImplementation.ActiveDirectory)]
    //[InlineData("FreeIpaCredentials.txt", "|", LdapImplementation.FreeIPA)]
    public async Task LdapFirstFactorProcessor_CorrectCredentials_ShouldAccept(string config, string separator, LdapImplementation ldapImplementation)
    {
        //Arrange
        var sensitiveData = GetConfig(config, separator);
        var formatterProviderMock = new LdapBindNameFormatterProvider([new ActiveDirectoryFormatter(), new FreeIpaFormatter()]);

        var processor = new LdapFirstFactorProcessor(new CustomLdapConnectionFactory(), formatterProviderMock, NullLogger<LdapFirstFactorProcessor>.Instance);

        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        var packetMock = new Mock<IRadiusPacket>();
        packetMock.Setup(x => x.UserName).Returns(sensitiveData["UserName"]);
        packetMock.Setup(x => x.TryGetUserPassword()).Returns(sensitiveData["Password"]);
        contextMock.Setup(x => x.RequestPacket).Returns(packetMock.Object);

        var serverSettings = new Mock<ILdapServerConfiguration>();
        serverSettings.Setup(x => x.ConnectionString).Returns(sensitiveData["ConnectionString"]);
        serverSettings.Setup(x => x.BindTimeoutInSeconds).Returns(30);
        contextMock.Setup(x => x.LdapServerConfiguration).Returns(serverSettings.Object);
        
        var authState = new AuthenticationState();
        contextMock.Setup(x => x.AuthenticationState).Returns(authState);

        var transformRules = new UserNameTransformRules();
        contextMock.Setup(x => x.UserNameTransformRules).Returns(transformRules);
        contextMock.Setup(x => x.PreAuthnMode).Returns(PreAuthModeDescriptor.Default);
        contextMock.Setup(x => x.Passphrase).Returns(UserPassphrase.Parse(sensitiveData["Password"], PreAuthModeDescriptor.Default));
        contextMock.Setup(x => x.LdapSchema.LdapServerImplementation).Returns(ldapImplementation);
        var profile = new Mock<ILdapProfile>();
        profile.Setup(x => x.Dn).Returns(new DistinguishedName(sensitiveData["UserDn"]));
        contextMock.Setup(x => x.UserLdapProfile).Returns(profile.Object);
        
        //Act
        await processor.ProcessFirstFactor(contextMock.Object);
        
        //Assert
        Assert.Equal(AuthenticationStatus.Accept, authState.FirstFactorStatus);
    }

    [Theory]
    [InlineData("ActiveDirectoryCredentials.txt", "|")]
    [InlineData("FreeIpaCredentials.txt", "|")]
    public async Task LdapFirstFactorProcessor_IncorrectPassword_ShouldReject(string config, string separator)
    {
        //Arrange
        var sensitiveData = GetConfig(config, separator);
        var formatterProviderMock = new LdapBindNameFormatterProvider([]);
        var processor = new LdapFirstFactorProcessor(new CustomLdapConnectionFactory(), formatterProviderMock, NullLogger<LdapFirstFactorProcessor>.Instance);
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
        
        //Act
        await processor.ProcessFirstFactor(contextMock.Object);
        
        //Assert
        Assert.Equal(AuthenticationStatus.Reject, authState.FirstFactorStatus);
    }

    [Theory]
    [InlineData("ActiveDirectoryCredentials.txt", "|")]
    [InlineData("FreeIpaCredentials.txt", "|")]
    public async Task LdapFirstFactorProcessor_IncorrectLogin_ShouldReject(string config, string separator)
    {
        //Arrange
        var sensitiveData = GetConfig(config, separator);
        var formatterProviderMock = new LdapBindNameFormatterProvider([]);
        var processor = new LdapFirstFactorProcessor(new CustomLdapConnectionFactory(), formatterProviderMock, NullLogger<LdapFirstFactorProcessor>.Instance);
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
        
        //Act
        await processor.ProcessFirstFactor(contextMock.Object);
        
        //Assert
        Assert.Equal(AuthenticationStatus.Reject, authState.FirstFactorStatus);
    }

    private Dictionary<string, string> GetConfig(string config, string separator)
    {
        return ConfigUtils.GetConfigSensitiveData(config, separator);
    }
}