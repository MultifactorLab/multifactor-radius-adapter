using System.Net;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Parser;

namespace Multifactor.Radius.Adapter.v2.Tests.Infrastructure.Configurations;

public class ConfigurationValueParserIpEntriesTests
{
    [Theory]
    [InlineData("192.168.0.10", "192.168.0.10", true)]
    [InlineData("192.168.0.10", "192.168.0.11", false)]
    [InlineData("192.168.0.1-192.168.0.5", "192.168.0.5", true)]
    [InlineData("192.168.0.1-192.168.0.5", "192.168.0.6", false)]
    [InlineData("10.10.0.0/16", "10.10.255.255", true)]
    [InlineData("10.10.0.0/16", "10.11.0.0", false)]
    [InlineData("2001:db8::1", "2001:db8::1", true)]
    [InlineData("2001:db8::1-2001:db8::5", "2001:db8::5", true)]
    [InlineData("2001:db8::1-2001:db8::5", "2001:db8::6", false)]
    [InlineData("2001:db8::/64", "2001:db8::abcd", true)]
    [InlineData(" 192.168.0.1 - 192.168.0.5 ; 10.0.0.0 / 24 ;", "10.0.0.7", true)]
    public void ParsesSingleAddressesRangesAndCidr(string rules, string address, bool expected)
    {
        Assert.True(ConfigurationValueParser.TryParseIpEntries(rules, out var entries));
        Assert.Equal(expected, IpEntry.Matches(entries!, IPAddress.Parse(address)));
    }

    [Fact]
    public void ParsesExampleFromDocumentation()
    {
        Assert.True(ConfigurationValueParser.TryParseIpEntries("192.168.0.1-192.168.0.5; 192.168.0.10; 10.0.0.0/24",
            out var entries));
        Assert.Equal(3, entries!.Length);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-ip")]
    [InlineData("192.168.0.1; not-an-ip")]
    [InlineData("192.168.0.1-")]
    [InlineData("10.0.0.0/33")]
    [InlineData("10.0.0.0/-1")]
    [InlineData("2001:db8::/129")]
    [InlineData("10.0.0.0/abc")]
    public void UnparsableRulesFail(string? rules)
    {
        Assert.False(ConfigurationValueParser.TryParseIpEntries(rules, out _));
    }
}
