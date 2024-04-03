﻿using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Framework.Context;
using MultiFactor.Radius.Adapter.Server;
using MultiFactor.Radius.Adapter.Server.Pipeline.AccessChallenge;
using MultiFactor.Radius.Adapter.Services.Ldap;
using MultiFactor.Radius.Adapter.Services.Ldap.Profile;
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
                RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636)
            };
            context.SetChallengeState(reqId);

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
                RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636)
            };
            context.SetChallengeState(reqId);

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
            var packet = RadiusPacketFactory.AccessChallenge();
            packet.AddAttribute("User-Password", string.Empty);
            packet.AddAttribute("User-Name", "UserName");
            var context = new RadiusContext(packet, client, new Mock<IUdpClient>().Object, new Mock<IServiceProvider>().Object)
            {

                RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636)
            };
            context.SetChallengeState(reqId);

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
            var packet = RadiusPacketFactory.AccessChallenge();
            packet.AddAttribute("MS-CHAP2-Response", (string?)null);
            packet.AddAttribute("User-Name", "UserName");
            var context = new RadiusContext(packet, client, new Mock<IUdpClient>().Object, new Mock<IServiceProvider>().Object)
            {
                RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636)
            };
            context.SetChallengeState(reqId);

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
            var packet = RadiusPacketFactory.AccessChallenge();
            packet.AddAttribute(attr, "value");
            packet.AddAttribute("User-Name", "UserName");
            var context = new RadiusContext(packet, client, new Mock<IUdpClient>().Object, new Mock<IServiceProvider>().Object)
            {
                RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636)
            };
            context.SetChallengeState(reqId);
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
                .ReturnsAsync(new ChallengeResponse(AuthenticationCode.Accept));
            var logger = new Mock<ILogger<SecondFactorChallengeProcessor>>();
            var processor = new SecondFactorChallengeProcessor(adapter.Object, logger.Object);

            var client = new ClientConfiguration("cli_config", "rds", AuthenticationSource.None, "key", "secret");
            var packet = RadiusPacketFactory.AccessChallenge();
            packet.AddAttribute("User-Password", "pass");
            packet.AddAttribute("User-Name", "UserName");
            var context = new RadiusContext(packet, client, new Mock<IUdpClient>().Object, new Mock<IServiceProvider>().Object)
            {
                RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636),
                UserGroups = new List<string>
                {
                    "User Group 1",
                    "User Group 2"
                },
            };
            context.SetChallengeState(reqId);

            var testDn = "CN=User Name,CN=Users,DC=domain,DC=local";
            var attrs = new LdapAttributes().Add("sAmaccountName", "user name");
            context.UpdateProfile(new LdapProfile(LdapIdentity.BaseDn(testDn), attrs, Array.Empty<string>(), null).SetIdentityAttribute("multifactor"));
            processor.AddState(context);
            var newPacket = RadiusPacketFactory.AccessChallenge();
            newPacket.AddAttribute("User-Password", "pass");
            newPacket.AddAttribute("User-Name", "UserName");
            var newContext = new RadiusContext(newPacket, client, new Mock<IUdpClient>().Object, new Mock<IServiceProvider>().Object)
            {
                RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636)
            };

            var result = await processor.ProcessChallengeAsync(new ChallengeRequestIdentifier(client.Name, reqId), newContext);

            Assert.Equal(ChallengeCode.Accept, result);

            newContext.UserGroups.Should().BeEquivalentTo(context.UserGroups);
            newContext.Profile.Attributes.Should().BeEquivalentTo(context.Profile.Attributes);
        }
    }
}
