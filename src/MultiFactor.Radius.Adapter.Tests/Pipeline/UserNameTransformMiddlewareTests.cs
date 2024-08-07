using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MultiFactor.Radius.Adapter.Core.Framework.Context;
using MultiFactor.Radius.Adapter.Core.Framework.Pipeline;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.RootLevel;
using MultiFactor.Radius.Adapter.Server;
using MultiFactor.Radius.Adapter.Server.Pipeline.AccessChallenge;
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
                builder.UseMiddleware<AccessChallengeMiddleware>();
                builder.UseMiddleware<FirstFactorTransformUserNameMiddleware>();
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                    x.ClientConfigFilePaths = new[] {
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

            var context = new RadiusContext(RadiusPacketFactory.AccessRequest(), config.Clients[0], new Mock<IServiceProvider>().Object)
            {
                OriginalUserName = from
            };

            var middleware = host.Service<FirstFactorTransformUserNameMiddleware>();
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
            var middleware = host.Service<FirstFactorTransformUserNameMiddleware>();
            var nextDelegate = new Mock<RadiusRequestDelegate>();
            context.Configuration.UserNameTransformRules.BeforeFirstFactor.Length.Should().BeGreaterThan(0);
            context.Configuration.UserNameTransformRules.BeforeSecondFactor.Length.Should().BeGreaterThan(0);
            await middleware.InvokeAsync(context, nextDelegate.Object);
            context.UserName.Should().BeEquivalentTo(to);
        }

    }
}
