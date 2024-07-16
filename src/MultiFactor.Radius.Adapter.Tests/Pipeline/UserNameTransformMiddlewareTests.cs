using Elastic.CommonSchema;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Framework.Context;
using MultiFactor.Radius.Adapter.Framework.Pipeline;
using MultiFactor.Radius.Adapter.Server;
using MultiFactor.Radius.Adapter.Server.Pipeline.TransformUserName;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;
using MultiFactor.Radius.Adapter.Tests.Fixtures.Radius;

namespace MultiFactor.Radius.Adapter.Tests.Pipeline
{
    public class UserNameTransformMiddlewareTests
    {
        private TestHost CreateHost(string asset)
        {
            return TestHostFactory.CreateHost(builder =>
            {
                builder.Services.RemoveService<IRootConfigurationProvider>().AddSingleton<IRootConfigurationProvider, TestRootConfigProvider>();
                builder.Services.RemoveService<IClientConfigurationsProvider>().AddSingleton<IClientConfigurationsProvider, TestClientConfigsProvider>();
                builder.Services.AddSingleton<TransformUserNameMiddleware>();
                builder.Services.Configure<TestConfigProviderOptions>(x =>
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

            var config = host.Service<IServiceConfiguration>();
            var responseSender = new Mock<IRadiusResponseSender>();
            var context = new RadiusContext(RadiusPacketFactory.AccessRequest(), config.Clients[0], new Mock<IServiceProvider>().Object)
            {
                OriginalUserName = from
            };

            var middleware = host.Service<TransformUserNameMiddleware>();
            var nextDelegate = new Mock<RadiusRequestDelegate>();
            await middleware.InvokeAsync(context, nextDelegate.Object);
            context.UserName.Should().BeEquivalentTo(to);
        }

        [Theory]
        [InlineData("username-transformation-rule-legacy.config", "first", "first@test.local")]
        public async Task Invoke_LegacyShouldChangeBothFactors(string asset, string from, string to)
        {
            var host = CreateHost(asset);
            var config = host.Service<IServiceConfiguration>();
            var responseSender = new Mock<IRadiusResponseSender>();
            var context = new RadiusContext(RadiusPacketFactory.AccessRequest(), config.Clients[0], new Mock<IServiceProvider>().Object)
            {
                OriginalUserName = from
            };
            var middleware = host.Service<TransformUserNameMiddleware>();
            var nextDelegate = new Mock<RadiusRequestDelegate>();
            context.Configuration.UserNameTransformRules.BeforeFirstFactor.Length.Should().BeGreaterThan(0);
            context.Configuration.UserNameTransformRules.BeforeSecondFactor.Length.Should().BeGreaterThan(0);
            await middleware.InvokeAsync(context, nextDelegate.Object);
            context.UserName.Should().BeEquivalentTo(to);

            middleware = host.Service<TransformUserNameMiddleware>();
        }

    }
}
