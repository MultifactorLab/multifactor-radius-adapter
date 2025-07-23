using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MultiFactor.Radius.Adapter.Core.Framework.Context;
using MultiFactor.Radius.Adapter.Core.Framework.Pipeline;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.ClientLevel;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Features.PreAuthModeFeature;
using MultiFactor.Radius.Adapter.Server.Pipeline.IpWhiteList;
using NetTools;

namespace MultiFactor.Radius.Adapter.Tests.Pipeline;

public class IpWhiteListMiddlewareTests
{
    [Fact]
    public async Task EmptyWhiteList_ShouldInvokeNextMiddleware()
    {
        var middleware = new IpWhiteListMiddleware(NullLogger<IpWhiteListMiddleware>.Instance);

        var context = CreateContext("127.0.0.1", []);
        var next = new Mock<RadiusRequestDelegate>();
        
        await middleware.InvokeAsync(context, next.Object);
        
        next.Verify(q => q.Invoke(It.Is<RadiusContext>(x => x == context)), Times.Once);
    }
    
    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("127.0.0.1/16")]
    [InlineData("127.0.0.3/24")]
    [InlineData("127.0.0.1-127.0.0.2")]
    [InlineData("126.0.0.1-127.0.0.2")]
    public async Task ClientIpInRange_ShouldInvokeNextMiddleware(string range)
    {
        var middleware = new IpWhiteListMiddleware(NullLogger<IpWhiteListMiddleware>.Instance);
        var ips = range.Split(";", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var context = CreateContext("127.0.0.1", ips);
        var next = new Mock<RadiusRequestDelegate>();
        
        await middleware.InvokeAsync(context, next.Object);
        
        next.Verify(q => q.Invoke(It.Is<RadiusContext>(x => x == context)), Times.Once);
    }
    
    [Theory]
    [InlineData("127.0.0.3")]
    [InlineData("192.168.0.1/16")]
    [InlineData("192.168.0.1/24")]
    [InlineData("127.0.0.2-127.0.0.5")]
    [InlineData("192.168.0.1-192.168.0.2")]
    public async Task ClientIpNotInRange_ShouldTerminate(string range)
    {
        var middleware = new IpWhiteListMiddleware(NullLogger<IpWhiteListMiddleware>.Instance);
        var ips = range.Split(";", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var context = CreateContext("127.0.0.1", ips);
        var next = new Mock<RadiusRequestDelegate>();
        
        await middleware.InvokeAsync(context, next.Object);
        
        next.Verify(q => q.Invoke(It.Is<RadiusContext>(x => x == context)), Times.Never);
        Assert.Equal(AuthenticationCode.Reject, context.Authentication.FirstFactor);
        Assert.Equal(AuthenticationCode.Reject, context.Authentication.SecondFactor);
    }
    
    [Theory]
    [InlineData("127.0.0.3")]
    [InlineData("192.168.0.1/16")]
    [InlineData("192.168.0.1/24")]
    [InlineData("127.0.0.2-127.0.0.5")]
    [InlineData("192.168.0.1-192.168.0.2")]
    public async Task CallingStationIdNotInRange_ShouldTerminate(string range)
    {
        var middleware = new IpWhiteListMiddleware(NullLogger<IpWhiteListMiddleware>.Instance);
        var ips = range.Split(";", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var context = CreateContext(clientIp: "127.0.0.3", ipWhiteList: ips, callingStationId: "127.0.0.1");
        var next = new Mock<RadiusRequestDelegate>();
        
        await middleware.InvokeAsync(context, next.Object);
        
        next.Verify(q => q.Invoke(It.Is<RadiusContext>(x => x == context)), Times.Never);
        Assert.Equal(AuthenticationCode.Reject, context.Authentication.FirstFactor);
        Assert.Equal(AuthenticationCode.Reject, context.Authentication.SecondFactor);
    }
    
    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("127.0.0.1/16")]
    [InlineData("127.0.0.3/24")]
    [InlineData("127.0.0.1-127.0.0.2")]
    [InlineData("126.0.0.1-127.0.0.2")]
    public async Task CallingStationIdInRange_ShouldInvokeNextMiddleware(string range)
    {
        var middleware = new IpWhiteListMiddleware(NullLogger<IpWhiteListMiddleware>.Instance);
        var ips = range.Split(";", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var context = CreateContext(clientIp: "198.0.0.3", ips, callingStationId: "127.0.0.1");
        var next = new Mock<RadiusRequestDelegate>();
        
        await middleware.InvokeAsync(context, next.Object);
        
        next.Verify(q => q.Invoke(It.Is<RadiusContext>(x => x == context)), Times.Once);
    }
    
    private RadiusContext CreateContext(string clientIp, string[] ipWhiteList, string callingStationId = null)
    {
        var packetMock = new Mock<IRadiusPacket>();
        packetMock.Setup(x => x.TryGetUserPassword()).Returns(string.Empty);
        packetMock.Setup(x => x.CallingStationId).Returns(callingStationId);
        var configMock = new Mock<IClientConfiguration>();
        configMock.Setup(x => x.IpWhiteAddressRanges).Returns(ipWhiteList.Select(IPAddressRange.Parse).ToList());
        configMock.Setup(x => x.PreAuthnMode).Returns(PreAuthModeDescriptor.Default);
        var providerMock = new Mock<IServiceProvider>();
        var context = new RadiusContext(packetMock.Object, configMock.Object, providerMock.Object);
        context.RemoteEndpoint = IPEndPoint.Parse(clientIp);
        return context;
    }
}