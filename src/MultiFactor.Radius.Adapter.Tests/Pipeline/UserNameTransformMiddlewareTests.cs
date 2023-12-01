using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Core.Pipeline;
using MultiFactor.Radius.Adapter.Server;
using MultiFactor.Radius.Adapter.Server.Pipeline;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;
using MultiFactor.Radius.Adapter.Tests.Fixtures.Radius;

namespace MultiFactor.Radius.Adapter.Tests.Pipeline
{
    public class UserNameTransformMiddlewareTests
    {
        private IHost CreateHost(string asset)
        {
            return TestHostFactory.CreateHost(services =>
            {
                services.RemoveService<IRootConfigurationProvider>().AddSingleton<IRootConfigurationProvider, TestRootConfigProvider>();
                services.RemoveService<IClientConfigurationsProvider>().AddSingleton<IClientConfigurationsProvider, TestClientConfigsProvider>();
                services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                    x.ClientConfigFilePaths = new[]
                    {
                        TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, asset)
                    };
                });
            });
        }

        [Theory]
        [InlineData("username-transformation-rule-before-first-fa.config", "first", "first@test.local")]
        public async Task Invoke_ShouldTransform(string asset, string from, string to)
        {
            var host = CreateHost(asset);

            var config = host.Services.GetRequiredService<IServiceConfiguration>();
            var responseSender = new Mock<IRadiusResponseSender>();
            var context = new RadiusContext(config.Clients[0], responseSender.Object, new Mock<IServiceProvider>().Object)
            {
                RequestPacket = RadiusPacketFactory.AccessRequest(),
                UserName = from,
                OriginalUserName = from
            };

            var middleware = host.Services.GetRequiredService<TransformUserNameMiddleware>();
            var nextDelegate = new Mock<RadiusRequestDelegate>();
            await middleware.InvokeAsync(context, nextDelegate.Object);
            context.UserName.Should().BeEquivalentTo(to);
        }

        [Theory]
        [InlineData("username-transformation-rule-legacy.config", "first", "first@test.local")]
        public async Task Invoke_LegacyShouldChangeBothFactors(string asset, string from, string to)
        {
            var host = CreateHost(asset);
            var config = host.Services.GetRequiredService<IServiceConfiguration>();
            var responseSender = new Mock<IRadiusResponseSender>();
            var context = new RadiusContext(config.Clients[0], responseSender.Object, new Mock<IServiceProvider>().Object)
            {
                RequestPacket = RadiusPacketFactory.AccessRequest(),
                UserName = from,
                OriginalUserName = from
            };

            var middleware = host.Services.GetRequiredService<TransformUserNameMiddleware>();
            var nextDelegate = new Mock<RadiusRequestDelegate>();
            context.ClientConfiguration.UserNameTransformRules.BeforeFirstFactor.Length.Should().BeGreaterThan(0);
            context.ClientConfiguration.UserNameTransformRules.BeforeSecondFactor.Length.Should().BeGreaterThan(0);
            await middleware.InvokeAsync(context, nextDelegate.Object);
            context.UserName.Should().BeEquivalentTo(to);
        }

    }
}
