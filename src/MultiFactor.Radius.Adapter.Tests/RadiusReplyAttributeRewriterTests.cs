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
        public void Rewrite_ResponseShoulContainKeys()
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
        public void Rewrite_Sufficient_ResponseShoulContainOneValue()
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
    }
}
