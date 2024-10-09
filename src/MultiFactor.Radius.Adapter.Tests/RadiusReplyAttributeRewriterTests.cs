using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MultiFactor.Radius.Adapter.Core.Framework.Context;
using MultiFactor.Radius.Adapter.Core.Radius.Attributes;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.ClientLevel;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.RootLevel;
using MultiFactor.Radius.Adapter.Server;
using MultiFactor.Radius.Adapter.Server.Pipeline.PostProcessing;
using MultiFactor.Radius.Adapter.Services.Ldap.Profile;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;
using MultiFactor.Radius.Adapter.Tests.Fixtures.Radius;

namespace MultiFactor.Radius.Adapter.Tests
{
    [Trait("Category", "Radius Reply Attributes")]
    public class RadiusReplyAttributeRewriterTests
    {
        [Fact]
        public void LoadConfig_SimpleAttribute_ShouldLoadCorrectly()
        {
            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-multi.config");
                    x.ClientConfigFilePaths = new[]
                    {
                        TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "radius-reply.config")
                    };
                });
            });

            var config = host.Service<IServiceConfiguration>();
            var cli = config.Clients[0];

            cli.RadiusReplyAttributes.Should().ContainSingle();
            cli.RadiusReplyAttributes.First().Should().BeEquivalentTo(new 
            { 
                Key = "Fortinet-Group-Name",
                Value = new[] { new RadiusReplyAttributeValue("Admins", null, false) }
            });
        }
        
        [Fact]
        public void LoadConfig_AttributeWithCondition_ShouldLoadCorrectly()
        {
            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-multi.config");
                    x.ClientConfigFilePaths = new[]
                    {
                        TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "radius-reply-with-condition.config")
                    };
                });
            });

            var config = host.Service<IServiceConfiguration>();
            var cli = config.Clients[0];

            cli.RadiusReplyAttributes.Should().ContainSingle();
            cli.RadiusReplyAttributes.First().Should().BeEquivalentTo(new 
            { 
                Key = "Fortinet-Group-Name",
                Value = new[] { new RadiusReplyAttributeValue("Admins", "UserGroup=VPN Admins", false) }
            });
        }
        
        [Fact]
        public void LoadConfig_AttributeWithAttribute_ShouldLoadCorrectly()
        {
            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-multi.config");
                    x.ClientConfigFilePaths = new[]
                    {
                        TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "radius-reply-with-attribute.config")
                    };
                });
            });
            
            var config = host.Service<IServiceConfiguration>();
            var cli = config.Clients[0];

            cli.RadiusReplyAttributes.Should().ContainSingle();
            cli.RadiusReplyAttributes.First().Should().BeEquivalentTo(new 
            { 
                Key = "Fortinet-Group-Name",
                Value = new[] { new RadiusReplyAttributeValue("displayName", false) }
            });
        }
        
        [Fact]
        public void LoadConfig_MultipleWithTheSameKey_ShouldLoadCorrectly()
        {
            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-multi.config");
                    x.ClientConfigFilePaths = new[]
                    {
                        TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "radius-reply-join.config")
                    };
                });
            });

            var config = host.Service<IServiceConfiguration>();
            var cli = config.Clients[0];

            cli.RadiusReplyAttributes.Should().ContainSingle();
            cli.RadiusReplyAttributes.First().Should().BeEquivalentTo(new 
            { 
                Key = "Fortinet-Group-Name",
                Value = new[] 
                { 
                    new RadiusReplyAttributeValue("Users", "UserGroup=VPN Users", true),
                    new RadiusReplyAttributeValue("Admins", "UserGroup=VPN Admins", false)
                }
            });
        }

        [Fact]
        public void LoadConfig_Sufficient_ShouldLoadCorrectly()
        {
            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-multi.config");
                    x.ClientConfigFilePaths = new[]
                    {
                        TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "radius-reply-with-sufficient.config")
                    };
                });
            });

            var config = host.Service<IServiceConfiguration>();
            var cli = config.Clients[0];

            cli.RadiusReplyAttributes.Should().ContainSingle();
            cli.RadiusReplyAttributes.First().Should().BeEquivalentTo(new
            {
                Key = "Fortinet-Group-Name",
                Value = new[] { new RadiusReplyAttributeValue("Admins", "UserGroup=VPN Admins", true) }
            });
        }

        [Fact]
        public void Rewrite_ResponseShouldContainKeys()
        {
            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });
            });

            var clientConfig = new ClientConfiguration("custom", "shared_secret", AuthenticationSource.ActiveDirectory, "key", "secret")
                .AddRadiusReplyAttribute("givenName", Array.Empty<RadiusReplyAttributeValue>())
                .AddRadiusReplyAttribute("displayName", Array.Empty<RadiusReplyAttributeValue>());
            var responseSender = new Mock<IRadiusResponseSender>();
            var context = host.CreateContext(requestPacket: RadiusPacketFactory.AccessRequest(), clientConfig: clientConfig, x =>
            {
                x.ResponsePacket = RadiusPacketFactory.AccessRequest();
            }); 
            var srv = host.Service<RadiusReplyAttributeEnricher>();
            srv.RewriteReplyAttributes(context);

            context.ResponsePacket.Attributes.Should().ContainKeys("givenName", "displayName");
        }   
        
        [Fact]
        public void Rewrite_Sufficient_ResponseShouldContainOneValue()
        {
            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });

                var dict = new Mock<IRadiusDictionary>();
                dict.Setup(x => x.GetAttribute(It.Is<string>(y => y == "givenName"))).Returns(new DictionaryAttribute("givenName", 26, DictionaryAttribute.TYPE_STRING));
                builder.Services.RemoveService<IRadiusDictionary>().AddSingleton(dict.Object);
            });

            var clientConfig = new ClientConfiguration("custom", "shared_secret", AuthenticationSource.ActiveDirectory, "key", "secret")
                .AddRadiusReplyAttribute("givenName", new[]
                {
                    new RadiusReplyAttributeValue("val1", null, true),
                    new RadiusReplyAttributeValue("val2", null)
                });
            var responseSender = new Mock<IRadiusResponseSender>();
            var context = host.CreateContext(requestPacket: RadiusPacketFactory.AccessRequest(), clientConfig: clientConfig, x =>
            {
                x.ResponsePacket = RadiusPacketFactory.AccessRequest();
            });

            var srv = host.Service<RadiusReplyAttributeEnricher>();
            srv.RewriteReplyAttributes(context);

            var givenName = context.ResponsePacket.Attributes["givenName"];
            givenName.Should().Contain("val1").And.NotContain("val2");
        }
         
        [Fact]
        public void Rewrite_ShouldPullValuesFromLdapAttr()
        {
            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });

                var dict = new Mock<IRadiusDictionary>();
                dict.Setup(x => x.GetAttribute(It.Is<string>(y => y == "givenName"))).Returns(new DictionaryAttribute("givenName", 26, DictionaryAttribute.TYPE_STRING));
                dict.Setup(x => x.GetAttribute(It.Is<string>(y => y == "displayName"))).Returns(new DictionaryAttribute("displayName", 26, DictionaryAttribute.TYPE_STRING));
                builder.Services.RemoveService<IRadiusDictionary>().AddSingleton(dict.Object);
            });

            var clientConfig = new ClientConfiguration("custom", "shared_secret", AuthenticationSource.ActiveDirectory, "key", "secret")
                .AddRadiusReplyAttribute("givenName", new[]
                {
                    new RadiusReplyAttributeValue("givenName")
                })
                .AddRadiusReplyAttribute("displayName", new[]
                {
                    new RadiusReplyAttributeValue("displayName")
                });
            var responseSender = new Mock<IRadiusResponseSender>();
            var context = host.CreateContext(requestPacket: RadiusPacketFactory.AccessRequest(), clientConfig: clientConfig, x =>
            {
                x.ResponsePacket = RadiusPacketFactory.AccessRequest();
            });
            var attrs = new LdapAttributes("CN=User Name,CN=Users,DC=domain,DC=local")
                .Add("givenName", "Given Name")
                .Add("displayName", "Display Name");
            context.Profile.UpdateAttributes(attrs);

            var srv = host.Service<RadiusReplyAttributeEnricher>();
            srv.RewriteReplyAttributes(context);

            var givenName = context.ResponsePacket.Attributes["givenName"];
            givenName.Should().ContainSingle("Given Name");
            
            var displayName = context.ResponsePacket.Attributes["displayName"];
            displayName.Should().ContainSingle("Display Name");
        }
        
        [Theory]
        [InlineData(int.MaxValue,"127.255.255.255")]
        [InlineData(int.MinValue,"128.0.0.0")]
        [InlineData(-1,"255.255.255.255")]
        [InlineData(-1407254008,"172.31.2.8")]
        [InlineData(-1062731775, "192.168.0.1")]
        [InlineData(0,"0.0.0.0")]
        [InlineData(123,"0.0.0.123")]
        public void Rewrite_ShouldParseMsRADIUSFramedIPAddress(int intValue, string expectedValue)
        {
            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });

                var dict = new Mock<IRadiusDictionary>();
                dict.Setup(x => x.GetAttribute(It.Is<string>(y => y == "Framed-IP-Address"))).Returns(new DictionaryAttribute("Framed-IP-Address", 8, DictionaryAttribute.TYPE_IPADDR));
                builder.Services.RemoveService<IRadiusDictionary>().AddSingleton(dict.Object);
            });

            var clientConfig = new ClientConfiguration("custom", "shared_secret", AuthenticationSource.ActiveDirectory, "key", "secret")
                .AddRadiusReplyAttribute("Framed-IP-Address", new[]
                {
                    new RadiusReplyAttributeValue("Framed-IP-Address")
                });
            
            var context = host.CreateContext(requestPacket: RadiusPacketFactory.AccessRequest(), clientConfig: clientConfig, x =>
            {
                x.ResponsePacket = RadiusPacketFactory.AccessRequest();
            });
            
            var attrs = new LdapAttributes("CN=User Name,CN=Users,DC=domain,DC=local")
                .Add("Framed-IP-Address", intValue.ToString());
            
            context.Profile.UpdateAttributes(attrs);

            var srv = host.Service<RadiusReplyAttributeEnricher>();
            srv.RewriteReplyAttributes(context);

            var responseAttrs = context.ResponsePacket.Attributes["Framed-IP-Address"];
            Assert.Single(responseAttrs);
            var attr = responseAttrs.First().ToString();
            Assert.Equal(expectedValue, attr);
        }
    }
}
