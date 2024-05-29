using FluentAssertions;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Exceptions;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;
using MultiFactor.Radius.Adapter.Infrastructure.Logging;

namespace MultiFactor.Radius.Adapter.Tests
{
    public class SerilogLoggerFactoryTests
    {
        [Fact]
        [Trait("Category", "logging-level")]
        public void CreateLogger_InvalidLoggingSettings_ShouldThrow()
        {
            var act = () =>
            {
                var rootConfig = TestRootConfigProvider.GetRootConfiguration(new TestConfigProviderOptions
                {
                    RootConfigFilePath = TestEnvironment.GetAssetPath("root-empty-logging-level.config")
                });
                var logger = SerilogLoggerFactory.CreateLogger(rootConfig);
            };

            act.Should().Throw<InvalidConfigurationException>()
                .WithMessage("Configuration error: 'logging-level' element not found. Config name: 'General'");
        }
    }
}
