using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Service;
using Multifactor.Radius.Adapter.v2.Core.Pipeline;
using Multifactor.Radius.Adapter.v2.Core.Radius;
using Multifactor.Radius.Adapter.v2.Core.Radius.Packet;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;
using Multifactor.Radius.Adapter.v2.Server;
using Multifactor.Radius.Adapter.v2.Server.Udp;
using Multifactor.Radius.Adapter.v2.Services.Radius;

namespace Multifactor.Radius.Adapter.v2.Tests.Server;

public class UdpPacketHandlerTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(15)]
    [InlineData(30)]
    [InlineData(30000)]
    [InlineData(60000)]
    public async Task MultipleRequests_ShouldProcess(int connectionsCount)
    {
        var configMock = new Mock<IServiceConfiguration>();
        var clientConfigMock = new Mock<IClientConfiguration>();
        clientConfigMock.Setup(x => x.RadiusSharedSecret).Returns("secret");
        var pipelineProviderMock = new Mock<IPipelineProvider>();
        var pipelineMock = new PipelineMock();
        pipelineProviderMock.Setup(x => x.GetRadiusPipeline(It.IsAny<string>())).Returns(pipelineMock);
        var packetServiceMock = new Mock<IRadiusPacketService>();
        packetServiceMock
            .Setup(x => x.Parse(It.IsAny<byte[]>(), It.IsAny<SharedSecret>(), It.IsAny<RadiusAuthenticator>()))
            .Returns(() => new RadiusPacket(new RadiusPacketHeader(PacketCode.AccessRequest, 1, new byte [16])));
        var nas = "nas";
        packetServiceMock.Setup(x => x.TryGetNasIdentifier(It.IsAny<byte[]>(), out nas)).Returns(true);
        configMock.Setup(x => x.GetClient(It.IsAny<string>())).Returns(clientConfigMock.Object);
        
        var handler = new UpdPacketHandler(configMock.Object, packetServiceMock.Object, pipelineProviderMock.Object, new Mock<IResponseSender>().Object, NullLogger<IUdpPacketHandler>.Instance);
        var tasks = new List<Task>();

        for(int i = 0; i < connectionsCount; i++)
        {
            var task = Task.Factory.StartNew(() => handler.HandleUdpPacket(new UdpReceiveResult(new byte[0], IPEndPoint.Parse("127.0.0.1:1812"))), TaskCreationOptions.LongRunning);
            tasks.Add(task);
        }
        
        await Task.WhenAll(tasks);
        foreach (var t in tasks)
        {
            Assert.True(t.IsCompletedSuccessfully);
        }
    }

    private class PipelineMock : IRadiusPipeline
    {
        private Random _random = new();
        public async Task ExecuteAsync(IRadiusPipelineExecutionContext context)
        {
            var delay = _random.Next(1, 15) * 1000;
            await Task.Delay(delay);
        }
    }
}