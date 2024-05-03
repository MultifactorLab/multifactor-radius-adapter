using Microsoft.Extensions.Configuration;
using MultiFactor.Radius.Adapter.Configuration.ConfigurationLoading;

namespace MultiFactor.Radius.Adapter.Tests.AdapterConfig
{
    [CollectionDefinition("Sequential")]
    public class SequentialCollection { }

    [Collection("Sequential")]
    public class SequentialTests : IDisposable
    {
        public void Dispose() { }
    }

    public class ConfigurationBuilderExtensionsTests : SequentialTests
    {
        [Fact]
        public void AddEnvironmentVariables_ShouldReturnVariable()
        {
            Environment.SetEnvironmentVariable("ANY_VAR", "888");

            var config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            var value = config.GetValue<string>("ANY_VAR");

            Assert.Equal("888", value);
        }

        [Fact]
        public void AddRadiusEnvironmentVariables_WithPrefix_ShouldReturnVariable()
        {
            Environment.SetEnvironmentVariable("RAD_ANY_VAR", "888");

            var config = new ConfigurationBuilder()
                .AddRadiusEnvironmentVariables()
                .Build();

            var value = config.GetValue<string>("ANY_VAR");

            Assert.Equal("888", value);
        }
          
        [Fact]
        public void AddRadiusEnvironmentVariables_WithConfigName_ShouldNotReturnVariable()
        {
            Environment.SetEnvironmentVariable("rad_ANY_VAR", "888");

            var config = new ConfigurationBuilder()
                .AddRadiusEnvironmentVariables("my")
                .Build();

            var value = config.GetValue<string>("RAD_ANY_VAR");

            Assert.Null(value);
        }
        
        [Fact]
        public void AddRadiusEnvironmentVariables_WithConfigName_ShouldReturnVariable()
        {
            Environment.SetEnvironmentVariable("rad_my_ANY_VAR", "888");

            var config = new ConfigurationBuilder()
                .AddRadiusEnvironmentVariables("my")
                .Build();

            var value = config.GetValue<string>("RAD_MY_ANY_VAR");

            Assert.Null(value);
        }

        [Fact]
        public void AddRadiusEnvironmentVariables_WithoutPrefix_ShouldNotReturnVariable()
        {
            Environment.SetEnvironmentVariable("ANY_VAR", "888");

            var config = new ConfigurationBuilder()
                .AddRadiusEnvironmentVariables()
                .Build();

            var value = config.GetValue<string>("ANY_VAR");

            Assert.Null(value);
        }
    }

    //public class C
    //{

    //    [Fact]
    //    public void AddRadiusEnvironmentVariables_WithoutPrefix_ShouldNotReturnVariable()
    //    {
    //        Environment.SetEnvironmentVariable("ANY_VAR", "888");

    //        var config = new ConfigurationBuilder()
    //            .AddRadiusEnvironmentVariables()
    //            .Build();

    //        var value = config.GetValue<string>("ANY_VAR");

    //        Assert.Null(value);
    //    }
    //}
}
