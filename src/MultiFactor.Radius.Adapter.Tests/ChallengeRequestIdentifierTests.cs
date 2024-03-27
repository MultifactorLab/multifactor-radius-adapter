using FluentAssertions;
using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Server;

namespace MultiFactor.Radius.Adapter.Tests
{
    [Trait("Category", "Challenge")]
    public class ChallengeRequestIdentifierTests
    {
        [Fact]
        public void Create_ShouldEqualToTheSame()
        {
            var cli = new ClientConfiguration("cli_config", "rds", AuthenticationSource.None, "key", "secret");

            var first = new ChallengeRequestIdentifier(cli.Name, "Qwerty123");
            var second = new ChallengeRequestIdentifier(cli.Name, "Qwerty123");

            first.Should().Be(second);
        }
        
        [Fact]
        public void Create_ShouldNotEqual()
        {
            var cli = new ClientConfiguration("cli_config", "rds", AuthenticationSource.None, "key", "secret");

            var first = new ChallengeRequestIdentifier(cli.Name, "Qwerty123");
            var second = new ChallengeRequestIdentifier(cli.Name, "Qwerty12345");

            first.Should().NotBe(second);
        }
        
        [Fact]
        public void Create_ShouldHasRequestId()
        {
            var cli = new ClientConfiguration("cli_config", "rds", AuthenticationSource.None, "key", "secret");

            var identifier = new ChallengeRequestIdentifier(cli.Name, "Qwerty123");

            identifier.RequestId.Should().Be("Qwerty123");
        }
    }
}
