using Microsoft.Extensions.Configuration;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.ConfigurationLoading;
using MultiFactor.Radius.Adapter.Tests.Fixtures;

namespace MultiFactor.Radius.Adapter.Tests.AdapterConfig
{
    [Trait("Category", "Adapter Configuration")]
    public class ConfigurationBuilderExtensionsTests
    {
        [Fact]
        public void AddEnvironmentVariables_ShouldReturnVariable()
        {
            TestEnvironmentVariables.With(env =>
            {
                env.SetEnvironmentVariable("ANY_VAR", "888");

                var config = new ConfigurationBuilder()
                    .AddEnvironmentVariables()
                    .Build();

                var value = config.GetValue<string>("ANY_VAR");

                Assert.Equal("888", value);
            });
        }

        [Fact]
        public void AddRadiusEnvironmentVariables_WithPrefix_ShouldReturnVariable()
        {
            TestEnvironmentVariables.With(env =>
            {
                env.SetEnvironmentVariable("RAD_ANY_VAR", "888");

                var config = new ConfigurationBuilder()
                    .AddRadiusEnvironmentVariables()
                    .Build();

                var value = config.GetValue<string>("ANY_VAR");

                Assert.Equal("888", value);
            });
        }
          
        [Fact]
        public void AddRadiusEnvironmentVariables_WithConfigName_ShouldNotReturnVariable()
        {
            TestEnvironmentVariables.With(env =>
            {
                env.SetEnvironmentVariable("rad_ANY_VAR", "888");

                var config = new ConfigurationBuilder()
                    .AddRadiusEnvironmentVariables("my")
                    .Build();

                var value = config.GetValue<string>("RAD_ANY_VAR");

                Assert.Null(value);
            });
        }
        
        [Fact]
        public void AddRadiusEnvironmentVariables_WithConfigName_ShouldReturnVariable()
        {
            TestEnvironmentVariables.With(env =>
            {
                Environment.SetEnvironmentVariable("rad_my_ANY_VAR", "888");

                var config = new ConfigurationBuilder()
                    .AddRadiusEnvironmentVariables("my")
                    .Build();

                var value = config.GetValue<string>("RAD_MY_ANY_VAR");

                Assert.Null(value);
            });
        }

        [Fact]
        public void AddRadiusEnvironmentVariables_WithoutPrefix_ShouldNotReturnVariable()
        {
            TestEnvironmentVariables.With(env =>
            {
                Environment.SetEnvironmentVariable("ANY_VAR", "888");

                var config = new ConfigurationBuilder()
                    .AddRadiusEnvironmentVariables()
                    .Build();

                var value = config.GetValue<string>("ANY_VAR");

                Assert.Null(value);
            });
        }
    }
}
