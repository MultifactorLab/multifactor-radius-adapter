using Multifactor.Radius.Adapter.v2.Core;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;

namespace Multifactor.Radius.Adapter.v2.Tests.ConfigurationTests;

public class LdapServerConfigurationTests
{
    [Fact]
    public void CreateDefaultLdapServerConfiguration_ShouldCreate()
    {
        var connection = "connection";
        var user = "user";
        var password = "password";
        var config = new LdapServerConfiguration(connection, user, password);

        Assert.NotNull(config);
        Assert.Equal(connection, config.ConnectionString);
        Assert.Equal(user, config.UserName);
        Assert.Equal(password, config.Password);
        Assert.Empty(config.AccessGroups);
        Assert.Empty(config.SecondFaGroups);
        Assert.Empty(config.SecondFaBypassGroups);
        Assert.Empty(config.NestedGroupsBaseDns);
        Assert.Empty(config.PhoneAttributes);
        Assert.False(config.LoadNestedGroups);
        Assert.Null(config.IdentityAttribute);
        Assert.Equal(0, config.BindTimeoutInSeconds);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SetLoadNestedGroups_ShouldSet(bool loadNestedGroups)
    {
        var connection = "connection";
        var user = "user";
        var password = "password";
        var config = new LdapServerConfiguration(connection, user, password);

        config.SetLoadNestedGroups(loadNestedGroups);

        Assert.Equal(loadNestedGroups, config.LoadNestedGroups);
    }

    [Fact]
    public void SetIdentityAttribute_ShouldSet()
    {
        var connection = "connection";
        var user = "user";
        var password = "password";
        var identity = "identity";
        var config = new LdapServerConfiguration(connection, user, password);

        config.SetIdentityAttribute(identity);

        Assert.Equal(identity, config.IdentityAttribute);
    }
    
    [Fact]
    public void SetBindTimeout_ShouldSet()
    {
        var connection = "connection";
        var user = "user";
        var password = "password";
        var timeout = 10;
        var config = new LdapServerConfiguration(connection, user, password);

        config.SetBindTimeoutInSeconds(timeout);

        Assert.Equal(timeout, config.BindTimeoutInSeconds);
    }
    
    [Fact]
    public void SetBindTimeout_InvalidTimeout_ShouldThrow()
    {
        var connection = "connection";
        var user = "user";
        var password = "password";
        var timeout = -1;
        var config = new LdapServerConfiguration(connection, user, password);

        Assert.Throws<ArgumentOutOfRangeException>(() => config.SetBindTimeoutInSeconds(timeout));
    }

    [Theory]
    [InlineData("group")]
    [InlineData("group1;group2")]
    [InlineData("group1;group2;group3")]
    [InlineData("group1;group2;group3;group4")]
    [InlineData("group1;group2;group3;group4;")]
    public void AddAccessGroups_ShouldAdd(string? groups)
    {
        var connection = "connection";
        var user = "user";
        var password = "password";
        var config = new LdapServerConfiguration(connection, user, password);
        var expectedGroups = Utils.SplitString(groups);

        config.AddAccessGroups(expectedGroups);

        Assert.NotNull(config.AccessGroups);
        Assert.True(expectedGroups.SequenceEqual(config.AccessGroups));
    }

    [Theory]
    [InlineData("group")]
    [InlineData("group1;group2")]
    [InlineData("group1;group2;group3")]
    [InlineData("group1;group2;group3;group4")]
    [InlineData("group1;group2;group3;group4;")]
    public void Add2FaGroups_ShouldAdd(string? groups)
    {
        var connection = "connection";
        var user = "user";
        var password = "password";
        var config = new LdapServerConfiguration(connection, user, password);
        var expectedGroups = Utils.SplitString(groups);
        config.AddSecondFaGroups(expectedGroups);
        Assert.NotNull(config.SecondFaGroups);
        Assert.True(expectedGroups.SequenceEqual(config.SecondFaGroups));
    }

    [Theory]
    [InlineData("group")]
    [InlineData("group1;group2")]
    [InlineData("group1;group2;group3")]
    [InlineData("group1;group2;group3;group4")]
    [InlineData("group1;group2;group3;group4;")]
    public void Add2FaBypassGroups_ShouldAdd(string? groups)
    {
        var connection = "connection";
        var user = "user";
        var password = "password";
        var config = new LdapServerConfiguration(connection, user, password);
        var expectedGroups = Utils.SplitString(groups);

        config.AddSecondFaBypassGroups(expectedGroups);

        Assert.NotNull(config.SecondFaBypassGroups);
        Assert.True(expectedGroups.SequenceEqual(config.SecondFaBypassGroups));
    }

    [Theory]
    [InlineData("group")]
    [InlineData("group1;group2")]
    [InlineData("group1;group2;group3")]
    [InlineData("group1;group2;group3;group4")]
    [InlineData("group1;group2;group3;group4;")]
    public void AddPhoneAttributes_ShouldAdd(string? groups)
    {
        var connection = "connection";
        var user = "user";
        var password = "password";
        var config = new LdapServerConfiguration(connection, user, password);
        var expectedGroups = Utils.SplitString(groups);

        config.AddPhoneAttributes(expectedGroups);

        Assert.NotNull(config.PhoneAttributes);
        Assert.True(expectedGroups.SequenceEqual(config.PhoneAttributes));
    }

    [Theory]
    [InlineData("group")]
    [InlineData("group1;group2")]
    [InlineData("group1;group2;group3")]
    [InlineData("group1;group2;group3;group4")]
    [InlineData("group1;group2;group3;group4;")]
    public void AddNestedGroupBaseDns_ShouldAdd(string? groups)
    {
        var connection = "connection";
        var user = "user";
        var password = "password";
        var config = new LdapServerConfiguration(connection, user, password);
        var expectedGroups = Utils.SplitString(groups);

        config.AddNestedGroupBaseDns(expectedGroups);

        Assert.NotNull(config.NestedGroupsBaseDns);
        Assert.True(expectedGroups.SequenceEqual(config.NestedGroupsBaseDns));
    }
}