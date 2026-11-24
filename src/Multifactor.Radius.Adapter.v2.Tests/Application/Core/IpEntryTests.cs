using System.Net;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;

namespace Multifactor.Radius.Adapter.v2.Tests.Application.Core;

public class IpEntryTests
{
    [Theory]
    [InlineData("192.168.0.1", 32, "192.168.0.1", true)]
    [InlineData("192.168.0.1", 32, "192.168.0.2", false)]
    [InlineData("10.10.0.0", 16, "10.10.255.255", true)]
    [InlineData("10.10.0.0", 16, "10.11.0.0", false)]
    [InlineData("0.0.0.0", 0, "203.0.113.5", true)]
    [InlineData("2001:db8::", 64, "2001:db8::abcd", true)]
    [InlineData("2001:db8::", 64, "2001:db8:0:1::1", false)]
    [InlineData("2001:db8::1", 128, "2001:db8::1", true)]
    [InlineData("2001:db8::1", 128, "2001:db8::2", false)]
    [InlineData("::", 0, "2001:db8::1", true)]
    public void CidrMatchesNetwork(string network, int prefix, string address, bool expected)
    {
        var entry = IpEntry.Cidr(IPAddress.Parse(network), prefix);
        Assert.Equal(expected, entry.Matches(IPAddress.Parse(address)));
    }

    [Theory]
    [InlineData("192.168.0.1", "192.168.0.5", "192.168.0.1", true)]
    [InlineData("192.168.0.1", "192.168.0.5", "192.168.0.5", true)]
    [InlineData("192.168.0.1", "192.168.0.5", "192.168.0.6", false)]
    [InlineData("192.168.0.1", "192.168.0.5", "192.168.0.0", false)]
    public void RangeIsInclusive(string start, string end, string address, bool expected)
    {
        var entry = IpEntry.Range(IPAddress.Parse(start), IPAddress.Parse(end));
        Assert.Equal(expected, entry.Matches(IPAddress.Parse(address)));
    }

    [Fact]
    public void Ipv4RuleDoesNotMatchIpv6Address()
    {
        var entry = IpEntry.Cidr(IPAddress.Parse("0.0.0.0"), 0);
        Assert.False(entry.Matches(IPAddress.Parse("::1")));
    }
}
