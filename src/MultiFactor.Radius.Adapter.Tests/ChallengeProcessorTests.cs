using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Server;
using MultiFactor.Radius.Adapter.Services.Ldap;
using MultiFactor.Radius.Adapter.Services.Ldap.ProfileLoading;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi;
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

            var api = new Mock<IMultiFactorApiClient>();
            var logger = new Mock<ILogger<ChallengeProcessor>>();
            var processor = new ChallengeProcessor(api.Object, logger.Object);

            var client = new ClientConfiguration("cli_config", "rds", AuthenticationSource.None, "key", "secret");
            var identifier = new ChallengeRequestIdentifier(client, reqId);
            var responseSender = new Mock<IRadiusResponseSender>();
            var context = new RadiusContext(client, responseSender.Object, new Mock<IServiceProvider>().Object)
            {
                RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636),
            };

            processor.AddState(identifier, context);

            processor.HasState(new ChallengeRequestIdentifier(client, reqId)).Should().BeTrue();
        }

        [Fact]
        public async Task ProcessChallenge_UsernameIsEmpty_ShouldReturnReject()
        {
            const string reqId = "RequestId";

            var api = new Mock<IMultiFactorApiClient>();
            var logger = new Mock<ILogger<ChallengeProcessor>>();
            var processor = new ChallengeProcessor(api.Object, logger.Object);

            var client = new ClientConfiguration("cli_config", "rds", AuthenticationSource.None, "key", "secret");
            var identifier = new ChallengeRequestIdentifier(client, reqId);
            var responseSender = new Mock<IRadiusResponseSender>();
            var context = new RadiusContext(client, responseSender.Object, new Mock<IServiceProvider>().Object)
            {
                RequestPacket = RadiusPacketFactory.AccessChallenge(),
                RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636)
            };
            processor.AddState(identifier, context);

            var result = await processor.ProcessChallengeAsync(identifier, context);

            result.Should().Be(PacketCode.AccessReject);
        }
        
        [Fact]
        public async Task ProcessChallenge_PapAndUserPasswordIsEmpty_ShouldReturnReject()
        {
            const string reqId = "RequestId";

            var api = new Mock<IMultiFactorApiClient>();
            var logger = new Mock<ILogger<ChallengeProcessor>>();
            var processor = new ChallengeProcessor(api.Object, logger.Object);

            var client = new ClientConfiguration("cli_config", "rds", AuthenticationSource.None, "key", "secret");
            var identifier = new ChallengeRequestIdentifier(client, reqId);
            var responseSender = new Mock<IRadiusResponseSender>();
            var context = new RadiusContext(client, responseSender.Object, new Mock<IServiceProvider>().Object)
            {
                RequestPacket = RadiusPacketFactory.AccessChallenge(x =>
                {
                    x.AddAttribute("User-Password", string.Empty);
                }),
                RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636),
                UserName = "UserName"
            };
            processor.AddState(identifier, context);

            var result = await processor.ProcessChallengeAsync(identifier, context);

            result.Should().Be(PacketCode.AccessReject);
        }
        
        [Fact]
        public async Task ProcessChallenge_MSCHAP2AndMSCHAP2ResponseAttrIsNull_ShouldReturnReject()
        {
            const string reqId = "RequestId";

            var api = new Mock<IMultiFactorApiClient>();
            var logger = new Mock<ILogger<ChallengeProcessor>>();
            var processor = new ChallengeProcessor(api.Object, logger.Object);

            var client = new ClientConfiguration("cli_config", "rds", AuthenticationSource.None, "key", "secret");
            var identifier = new ChallengeRequestIdentifier(client, reqId);
            var responseSender = new Mock<IRadiusResponseSender>();
            var context = new RadiusContext(client, responseSender.Object, new Mock<IServiceProvider>().Object)
            {
                RequestPacket = RadiusPacketFactory.AccessChallenge(x =>
                {
                    x.AddAttribute("MS-CHAP2-Response", (string?)null);
                }),
                RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636),
                UserName = "UserName"
            };
            processor.AddState(identifier, context);

            var result = await processor.ProcessChallengeAsync(identifier, context);

            result.Should().Be(PacketCode.AccessReject);
        }
        
        [Theory]
        [InlineData("EAP-Message")]
        [InlineData("CHAP-Password")]
        [InlineData("MS-CHAP-Response")]
        public async Task ProcessChallenge_UnsupportedAuthType_ShouldReturnReject(string attr)
        {
            const string reqId = "RequestId";

            var api = new Mock<IMultiFactorApiClient>();
            var logger = new Mock<ILogger<ChallengeProcessor>>();
            var processor = new ChallengeProcessor(api.Object, logger.Object);

            var client = new ClientConfiguration("cli_config", "rds", AuthenticationSource.None, "key", "secret");
            var identifier = new ChallengeRequestIdentifier(client, reqId);
            var responseSender = new Mock<IRadiusResponseSender>();
            var context = new RadiusContext(client, responseSender.Object, new Mock<IServiceProvider>().Object)
            {
                RequestPacket = RadiusPacketFactory.AccessChallenge(x =>
                {
                    x.AddAttribute(attr, "value");
                }),
                RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636),
                UserName = "UserName"
            };
            processor.AddState(identifier, context);

            var result = await processor.ProcessChallengeAsync(identifier, context);

            result.Should().Be(PacketCode.AccessReject);
        }

        [Fact]
        public async Task ProcessChallenge_PapAndApiReturnsAccept_ShouldReturnAcceptAndCopyValues()
        {
            const string reqId = "RequestId";

            var api = new Mock<IMultiFactorApiClient>();
            api.Setup(x => x.Challenge(It.IsAny<RadiusContext>(), It.IsAny<string>(), It.IsAny<ChallengeRequestIdentifier>()))
                .ReturnsAsync(PacketCode.AccessAccept);
            var logger = new Mock<ILogger<ChallengeProcessor>>();
            var processor = new ChallengeProcessor(api.Object, logger.Object);

            var client = new ClientConfiguration("cli_config", "rds", AuthenticationSource.None, "key", "secret");
            var responseSender = new Mock<IRadiusResponseSender>();
            var context = new RadiusContext(client, responseSender.Object, new Mock<IServiceProvider>().Object)
            {
                RequestPacket = RadiusPacketFactory.AccessChallenge(x =>
                {
                    x.AddAttribute("User-Password", "pass");
                }),
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
                }
            };
            var testDn = "CN=User Name,CN=Users,DC=domain,DC=local";
            context.SetProfile(LdapProfile.CreateBuilder(LdapIdentity.BaseDn(testDn), testDn).SetIdentityAttribute("multifactor").Build());
            processor.AddState(new ChallengeRequestIdentifier(client, reqId), context);

            var newContext = new RadiusContext(client, responseSender.Object, new Mock<IServiceProvider>().Object)
            {
                RequestPacket = RadiusPacketFactory.AccessChallenge(x =>
                {
                    x.AddAttribute("User-Password", "pass");
                }),
                RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636),
                UserName = "UserName"
            };

            var result = await processor.ProcessChallengeAsync(new ChallengeRequestIdentifier(client, reqId), newContext);

            result.Should().Be(PacketCode.AccessAccept);

            newContext.UserGroups.Should().BeEquivalentTo(context.UserGroups);
            newContext.LdapAttrs.Should().BeEquivalentTo(context.LdapAttrs);
        }
    }
}
