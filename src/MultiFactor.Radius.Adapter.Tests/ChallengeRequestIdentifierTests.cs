using FluentAssertions;
using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Server;

namespace MultiFactor.Radius.Adapter.Tests
{
    public class ChallengeRequestIdentifierTests
    {
        [Fact]
        public void Create_ShouldEqualToTheSame()
        {
            var cli = ClientConfiguration.CreateBuilder("cli_config", "rds", AuthenticationSource.None, "key", "secret").Build();

            var first = new ChallengeRequestIdentifier(cli, "Qwerty123");
            var second = new ChallengeRequestIdentifier(cli, "Qwerty123");

            first.Should().Be(second);
        }
        
        [Fact]
        public void Create_ShouldNotEqual()
        {
            var cli = ClientConfiguration.CreateBuilder("cli_config", "rds", AuthenticationSource.None, "key", "secret").Build();

            var first = new ChallengeRequestIdentifier(cli, "Qwerty123");
            var second = new ChallengeRequestIdentifier(cli, "Qwerty12345");

            first.Should().NotBe(second);
        }
        
        [Fact]
        public void Create_ShouldHasRequestId()
        {
            var cli = ClientConfiguration.CreateBuilder("cli_config", "rds", AuthenticationSource.None, "key", "secret").Build();

            var identifier = new ChallengeRequestIdentifier(cli, "Qwerty123");

            identifier.RequestId.Should().Be("Qwerty123");
        }
    }
}
