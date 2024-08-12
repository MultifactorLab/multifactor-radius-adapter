using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MultiFactor.Radius.Adapter.Core;
using MultiFactor.Radius.Adapter.Core.Framework.Context;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.RootLevel;
using MultiFactor.Radius.Adapter.Server.Pipeline.AccessChallenge;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;
using MultiFactor.Radius.Adapter.Tests.Fixtures.Radius;

namespace MultiFactor.Radius.Adapter.Tests
{
    public class UserNameTransformTests
    {
        private TestHost CreateHost(string asset)
        {
            return TestHostFactory.CreateHost(builder =>
            {
                builder.UseMiddleware<AccessChallengeMiddleware>();
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
        [InlineData("username-transformation-rule-before-first-fa.config", "first", "first@test.local", "first@tes1t.local")]
        public void Invoke_ShouldTransform(string asset, string from, string toFirst, string toSecond)
        {
            var host = CreateHost(asset);

            var config = host.Service<IServiceConfiguration>();

            var context = new RadiusContext(RadiusPacketFactory.AccessRequest(), config.Clients[0], new Mock<IServiceProvider>().Object)
            {
            };

            context.Configuration.UserNameTransformRules.BeforeFirstFactor.Length.Should().BeGreaterThan(0);
            context.Configuration.UserNameTransformRules.BeforeSecondFactor.Length.Should().BeGreaterThan(0);
            var result = UserNameTransformation.Transform(from, context.Configuration.UserNameTransformRules.BeforeFirstFactor);
            result.Should().NotBeNull().And.BeEquivalentTo(toFirst);

            result = UserNameTransformation.Transform(from, context.Configuration.UserNameTransformRules.BeforeSecondFactor);
            result.Should().NotBeNull().And.BeEquivalentTo(toSecond);
        }

        [Theory]
        [InlineData("username-transformation-rule-legacy.config", "first", "first@test.local")]
        public void Invoke_LegacyShouldChangeBothFactors(string asset, string from, string to)
        {
            var host = CreateHost(asset);
            var config = host.Service<IServiceConfiguration>();
            var context = new RadiusContext(RadiusPacketFactory.AccessRequest(), config.Clients[0], new Mock<IServiceProvider>().Object)
            {
            };
            context.Configuration.UserNameTransformRules.BeforeFirstFactor.Length.Should().BeGreaterThan(0);
            context.Configuration.UserNameTransformRules.BeforeSecondFactor.Length.Should().BeGreaterThan(0);
            var result = UserNameTransformation.Transform(from, context.Configuration.UserNameTransformRules.BeforeSecondFactor);
            result.Should().NotBeNull().And.BeEquivalentTo(to);
            result = UserNameTransformation.Transform(from, context.Configuration.UserNameTransformRules.BeforeFirstFactor);
            result.Should().NotBeNull().And.BeEquivalentTo(to);
        }

    }
}
