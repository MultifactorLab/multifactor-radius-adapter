using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.FirstFactor;
using Multifactor.Radius.Adapter.v2.Core.FirstFactor.LdapAuth;
using Multifactor.Radius.Adapter.v2.Core.Radius;
using Multifactor.Radius.Adapter.v2.Core.Radius.Packet;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;

namespace Multifactor.Radius.Adapter.v2.Tests.Unit.FirstFactorAuth;

public class LdapFirstFactorProcessorTests
{
    [Fact]
    public async Task LdapFirstFactorProcessor_NoRequestPacket_ShouldThrow()
    {
        var authProviderMock = new Mock<ILdapAuthProvider>();
        var processor = new LdapFirstFactorProcessor(authProviderMock.Object, NullLogger<LdapFirstFactorProcessor>.Instance);

        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        
        contextMock.Setup(x => x.RequestPacket).Returns(() => null);
        
        await Assert.ThrowsAsync<ArgumentNullException>(() => processor.ProcessFirstFactor(contextMock.Object));
    }
    
    [Fact]
    public async Task LdapFirstFactorProcessor_NoLdapServerConfiguration_ShouldThrow()
    {
        var authProviderMock = new Mock<ILdapAuthProvider>();
        var processor = new LdapFirstFactorProcessor(authProviderMock.Object, NullLogger<LdapFirstFactorProcessor>.Instance);

        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        
        contextMock.Setup(x => x.LdapServerConfiguration).Returns(() => null);
        contextMock.Setup(x => x.RequestPacket).Returns(new Mock<IRadiusPacket>().Object);
        
        await Assert.ThrowsAsync<InvalidOperationException>(() => processor.ProcessFirstFactor(contextMock.Object));
    }
    
    [Fact]
    public async Task LdapFirstFactorProcessor_NoAuthProcessors_ShouldThrow()
    {
        var authProviderMock = new Mock<ILdapAuthProvider>();
        authProviderMock.Setup(x => x.GetLdapAuthProcessor(It.IsAny<AuthenticationType>())).Returns(() => null);
        
        var processor = new LdapFirstFactorProcessor(authProviderMock.Object, NullLogger<LdapFirstFactorProcessor>.Instance);

        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        
        contextMock.Setup(x => x.LdapServerConfiguration).Returns(new Mock<ILdapServerConfiguration>().Object);
        contextMock.Setup(x => x.RequestPacket).Returns(new Mock<IRadiusPacket>().Object);
        
        await Assert.ThrowsAsync<InvalidOperationException>(() => processor.ProcessFirstFactor(contextMock.Object));
    }
    
    [Fact]
    public async Task LdapFirstFactorProcessor_AuthFailed_ShouldReject()
    {
        var authProviderMock = new Mock<ILdapAuthProvider>();
        var authProcessorMock = new Mock<ILdapAuthProcessor>();
        authProcessorMock.Setup(x=> x.Auth(It.IsAny<IRadiusPipelineExecutionContext>())).ReturnsAsync(new AuthResult() { IsSuccess = false });
        authProviderMock.Setup(x => x.GetLdapAuthProcessor(It.IsAny<AuthenticationType>())).Returns(authProcessorMock.Object);
        
        var processor = new LdapFirstFactorProcessor(authProviderMock.Object, NullLogger<LdapFirstFactorProcessor>.Instance);

        var contextMock = new Mock<IRadiusPipelineExecutionContext>();

        var authState = new AuthenticationState();
        contextMock.Setup(x => x.LdapServerConfiguration).Returns(new Mock<ILdapServerConfiguration>().Object);
        contextMock.Setup(x => x.RequestPacket).Returns(new Mock<IRadiusPacket>().Object);
        contextMock.Setup(x => x.AuthenticationState).Returns(authState);
        
        await processor.ProcessFirstFactor(contextMock.Object);
        Assert.Equal(AuthenticationStatus.Reject, authState.FirstFactorStatus);
    }
    
    [Fact]
    public async Task LdapFirstFactorProcessor_AuthSucceed_ShouldReject()
    {
        var authProviderMock = new Mock<ILdapAuthProvider>();
        var authProcessorMock = new Mock<ILdapAuthProcessor>();
        authProcessorMock.Setup(x=> x.Auth(It.IsAny<IRadiusPipelineExecutionContext>())).ReturnsAsync(new AuthResult() { IsSuccess = true });
        authProviderMock.Setup(x => x.GetLdapAuthProcessor(It.IsAny<AuthenticationType>())).Returns(authProcessorMock.Object);
        
        var processor = new LdapFirstFactorProcessor(authProviderMock.Object, NullLogger<LdapFirstFactorProcessor>.Instance);

        var contextMock = new Mock<IRadiusPipelineExecutionContext>();

        var authState = new AuthenticationState();
        contextMock.Setup(x => x.LdapServerConfiguration).Returns(new Mock<ILdapServerConfiguration>().Object);
        contextMock.Setup(x => x.RequestPacket).Returns(new Mock<IRadiusPacket>().Object);
        contextMock.Setup(x => x.AuthenticationState).Returns(authState);
        
        await processor.ProcessFirstFactor(contextMock.Object);
        Assert.Equal(AuthenticationStatus.Accept, authState.FirstFactorStatus);
    }
}