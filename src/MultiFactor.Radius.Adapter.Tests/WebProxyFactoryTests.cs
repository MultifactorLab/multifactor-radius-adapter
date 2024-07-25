using MultiFactor.Radius.Adapter.Infrastructure.Http;

namespace MultiFactor.Radius.Adapter.Tests
{
    [Trait("Category", "multifactor-api-proxy")]
    public class WebProxyFactoryTests
    {
        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData(" ")]
        [InlineData("192.168.1.1")]
        [InlineData("192.168.1.1:80")]
        public void TryCreateWebProxy_ShouldReturnFalse(string input)
        {
            Assert.False(WebProxyFactory.TryCreateWebProxy(input, out var _));
        }
        
        [Theory]
        [InlineData("http://192.168.1.1", "", "192.168.1.1", 80)]
        [InlineData("http://192.168.1.1:81", "", "192.168.1.1", 81)]
        [InlineData("https://192.168.1.1", "", "192.168.1.1", 443)]
        [InlineData("https://192.168.1.1:445", "", "192.168.1.1", 445)]
        [InlineData("https://user:password@www.contoso.com:80", "user:password", "www.contoso.com", 80)]
        [InlineData("http://login@suffix.com:password@www.proxy.com:3128", "login%40suffix.com:password", "www.proxy.com", 3128)]
        public void TryCreateWebProxy_ShouldReturnTrueAndCreateProxy(string inp, string user, string host, int port)
        {
            Assert.True(WebProxyFactory.TryCreateWebProxy(inp, out var proxy));
            Assert.NotNull(proxy);
            Assert.NotNull(proxy.Address);
            Assert.Equal(host, proxy.Address.Host);
            Assert.Equal(port, proxy.Address.Port);
            Assert.Equal(user, proxy.Address.UserInfo);
        }
    }
}
