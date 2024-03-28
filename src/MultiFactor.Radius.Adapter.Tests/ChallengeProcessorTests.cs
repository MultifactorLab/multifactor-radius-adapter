using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Moq;
using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Framework.Context;
using MultiFactor.Radius.Adapter.Server;
using MultiFactor.Radius.Adapter.Server.Pipeline.AccessChallenge;
using MultiFactor.Radius.Adapter.Services.Ldap;
using MultiFactor.Radius.Adapter.Services.Ldap.ProfileLoading;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi.Models;
using MultiFactor.Radius.Adapter.Tests.Fixtures.Radius;
using System.Net;

namespace MultiFactor.Radius.Adapter.Tests
{
    [Trait("Category", "Challenge")]
    public class ChallengeProcessorTests
    {
        [Fact]
        public void AddState_ShouldHasState()
        {
            const string reqId = "RequestId";

            var adapter = new Mock<IMultifactorApiAdapter>();
            var logger = new Mock<ILogger<SecondFactorChallengeProcessor>>();
            var processor = new SecondFactorChallengeProcessor(adapter.Object, logger.Object);

            var client = new ClientConfiguration("cli_config", "rds", AuthenticationSource.None, "key", "secret");
            var context = new RadiusContext(RadiusPacketFactory.AccessRequest(), client, new Mock<IUdpClient>().Object, new Mock<IServiceProvider>().Object)
            {
                RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636),
                State = reqId
            };

            processor.AddState(context);

            processor.HasState(new ChallengeRequestIdentifier(client.Name, reqId)).Should().BeTrue();
        }

        [Fact]
        public async Task ProcessChallenge_UsernameIsEmpty_ShouldReturnReject()
        {
            const string reqId = "RequestId";

            var adapter = new Mock<IMultifactorApiAdapter>();
            var logger = new Mock<ILogger<SecondFactorChallengeProcessor>>();
            var processor = new SecondFactorChallengeProcessor(adapter.Object, logger.Object);

            var client = new ClientConfiguration("cli_config", "rds", AuthenticationSource.None, "key", "secret");
            var identifier = new ChallengeRequestIdentifier(client.Name, reqId);
            var context = new RadiusContext(RadiusPacketFactory.AccessChallenge(), client, new Mock<IUdpClient>().Object, new Mock<IServiceProvider>().Object)
            {
                RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636),
                State = reqId
            };
            processor.AddState(context);

            var result = await processor.ProcessChallengeAsync(identifier, context);

            Assert.Equal(ChallengeCode.Reject, result);
        }
        
        [Fact]
        public async Task ProcessChallenge_PapAndUserPasswordIsEmpty_ShouldReturnReject()
        {
            const string reqId = "RequestId";

            var adapter = new Mock<IMultifactorApiAdapter>();
            var logger = new Mock<ILogger<SecondFactorChallengeProcessor>>();
            var processor = new SecondFactorChallengeProcessor(adapter.Object, logger.Object);

            var client = new ClientConfiguration("cli_config", "rds", AuthenticationSource.None, "key", "secret");
            var identifier = new ChallengeRequestIdentifier(client.Name, reqId);
            var packet = RadiusPacketFactory.AccessChallenge(x =>
            {
                x.AddAttribute("User-Password", string.Empty);
            });
            var context = new RadiusContext(packet, client, new Mock<IUdpClient>().Object, new Mock<IServiceProvider>().Object)
            {

                RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636),
                UserName = "UserName",
                State = reqId
            };
            processor.AddState(context);

            var result = await processor.ProcessChallengeAsync(identifier, context);

            Assert.Equal(ChallengeCode.Reject, result);
        }
        
        [Fact]
        public async Task ProcessChallenge_MSCHAP2AndMSCHAP2ResponseAttrIsNull_ShouldReturnReject()
        {
            const string reqId = "RequestId";

            var adapter = new Mock<IMultifactorApiAdapter>();
            var logger = new Mock<ILogger<SecondFactorChallengeProcessor>>();
            var processor = new SecondFactorChallengeProcessor(adapter.Object, logger.Object);

            var client = new ClientConfiguration("cli_config", "rds", AuthenticationSource.None, "key", "secret");
            var identifier = new ChallengeRequestIdentifier(client.Name, reqId);
            var packet = RadiusPacketFactory.AccessChallenge(x =>
            {
                x.AddAttribute("MS-CHAP2-Response", (string?)null);
            });
            var context = new RadiusContext(packet, client, new Mock<IUdpClient>().Object, new Mock<IServiceProvider>().Object)
            {
                RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636),
                UserName = "UserName",
                State = reqId
            };
            processor.AddState(context);

            var result = await processor.ProcessChallengeAsync(identifier, context);

            Assert.Equal(ChallengeCode.Reject, result);
        }
        
        [Theory]
        [InlineData("EAP-Message")]
        [InlineData("CHAP-Password")]
        [InlineData("MS-CHAP-Response")]
        public async Task ProcessChallenge_UnsupportedAuthType_ShouldReturnReject(string attr)
        {
            const string reqId = "RequestId";

            var adapter = new Mock<IMultifactorApiAdapter>();
            var logger = new Mock<ILogger<SecondFactorChallengeProcessor>>();
            var processor = new SecondFactorChallengeProcessor(adapter.Object, logger.Object);

            var client = new ClientConfiguration("cli_config", "rds", AuthenticationSource.None, "key", "secret");
            var identifier = new ChallengeRequestIdentifier(client.Name, reqId);
            var packet = RadiusPacketFactory.AccessChallenge(x =>
            {
                x.AddAttribute(attr, "value");
            });
            var context = new RadiusContext(packet, client, new Mock<IUdpClient>().Object, new Mock<IServiceProvider>().Object)
            {
                RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636),
                UserName = "UserName",
                State = reqId
            };
            processor.AddState(context);

            var result = await processor.ProcessChallengeAsync(identifier, context);

            Assert.Equal(ChallengeCode.Reject, result);
        }

        [Fact]
        public async Task ProcessChallenge_PapAndApiReturnsAccept_ShouldReturnAcceptAndCopyValues()
        {
            const string reqId = "RequestId";

            var adapter = new Mock<IMultifactorApiAdapter>();
            adapter.Setup(x => x.ChallengeAsync(It.IsAny<RadiusContext>(), It.IsAny<string>(), It.IsAny<ChallengeRequestIdentifier>()))
                .ReturnsAsync(new ChallengeResponse(PacketCode.AccessAccept));
            var logger = new Mock<ILogger<SecondFactorChallengeProcessor>>();
            var processor = new SecondFactorChallengeProcessor(adapter.Object, logger.Object);

            var client = new ClientConfiguration("cli_config", "rds", AuthenticationSource.None, "key", "secret");
            var packet = RadiusPacketFactory.AccessChallenge(x =>
            {
                x.AddAttribute("User-Password", "pass");
            });
            var context = new RadiusContext(packet, client, new Mock<IUdpClient>().Object, new Mock<IServiceProvider>().Object)
            {
                RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636),
                UserName = "UserName",
                UserGroups = new List<string>
                {
                    "User Group 1",
                    "User Group 2"
                },
                LdapAttrs = new Dictionary<string, object>
                {
                    { "sAmaccountName", "user name" }
                },
                State = reqId
            };
            var testDn = "CN=User Name,CN=Users,DC=domain,DC=local";
            context.SetProfile(LdapProfile.CreateBuilder(LdapIdentity.BaseDn(testDn), testDn).SetIdentityAttribute("multifactor").Build());
            processor.AddState(context);
            var newPacket = RadiusPacketFactory.AccessChallenge(x =>
            {
                x.AddAttribute("User-Password", "pass");
            });
            var newContext = new RadiusContext(newPacket, client, new Mock<IUdpClient>().Object, new Mock<IServiceProvider>().Object)
            {

                RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636),
                UserName = "UserName"
            };

            var result = await processor.ProcessChallengeAsync(new ChallengeRequestIdentifier(client.Name, reqId), newContext);

            Assert.Equal(ChallengeCode.Accept, result);

            newContext.UserGroups.Should().BeEquivalentTo(context.UserGroups);
            newContext.LdapAttrs.Should().BeEquivalentTo(context.LdapAttrs);
        }
    }
}
