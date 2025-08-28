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
        var config = GetDefaultConfiguration();

        config.SetLoadNestedGroups(loadNestedGroups);

        Assert.Equal(loadNestedGroups, config.LoadNestedGroups);
    }

    [Fact]
    public void SetIdentityAttribute_ShouldSet()
    {
        var config = GetDefaultConfiguration();
        var identity = "identity";
        config.SetIdentityAttribute(identity);

        Assert.Equal(identity, config.IdentityAttribute);
    }
    
    [Fact]
    public void SetBindTimeout_ShouldSet()
    {
        var config = GetDefaultConfiguration();
        var timeout = 10;

        config.SetBindTimeoutInSeconds(timeout);

        Assert.Equal(timeout, config.BindTimeoutInSeconds);
    }
    
    [Fact]
    public void SetBindTimeout_InvalidTimeout_ShouldThrow()
    {
        var config = GetDefaultConfiguration();
        var timeout = -1;

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
        var config = GetDefaultConfiguration();
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
        var config = GetDefaultConfiguration();
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
        var config = GetDefaultConfiguration();
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
        var config = GetDefaultConfiguration();
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
        var config = GetDefaultConfiguration();
        var expectedGroups = Utils.SplitString(groups);

        config.AddNestedGroupBaseDns(expectedGroups);

        Assert.NotNull(config.NestedGroupsBaseDns);
        Assert.True(expectedGroups.SequenceEqual(config.NestedGroupsBaseDns));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RequiresUpn_ShouldSet(bool value)
    {
        var config = GetDefaultConfiguration();

        config.RequiresUpn(value);
        
        Assert.Equal(value, config.UpnRequired);
    }
    
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EnableTrustedDomains_ShouldSetValue(bool value)
    {
        var config = GetDefaultConfiguration();

        config.EnableTrustedDomains(value);
        
        Assert.Equal(value, config.TrustedDomainsEnabled);
    }
    
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EnableAlternativeSuffixes_ShouldSetValue(bool value)
    {
        var config = GetDefaultConfiguration();

        config.EnableTrustedDomains(value);
        
        Assert.Equal(value, config.TrustedDomainsEnabled);
    }

    [Fact]
    public void SetDomainRules_ShouldSet()
    {
        var config = GetDefaultConfiguration();
        
        var rules = new PermissionRules(new List<string>(), new List<string>());
        config.SetDomainRules(rules);
        
        Assert.Equal(rules, config.DomainPermissions);
    }
    
    [Fact]
    public void SetAlternativeSuffixesRules_ShouldSet()
    {
        var config = GetDefaultConfiguration();
        var rules = new PermissionRules(new List<string>(), new List<string>());
        
        config.SetAlternativeSuffixesRules(rules);
        
        Assert.Equal(rules, config.SuffixesPermissions);
    }

    [Fact]
    public void Initialize_ShouldInitialize()
    {
        var config = GetDefaultConfiguration();
        var rules = new PermissionRules();
        var request = new LdapServerInitializeRequest()
        {
            PhoneAttributes = ["phone"],
            AccessGroups = ["access"],
            SecondFaGroups = ["2fa"],
            SecondFaBypassGroups = ["2fabypass"],
            NestedGroupsBaseDns = ["nested"],
            IdentityAttribute = "identity",
            LoadNestedGroups = true,
            BindTimeoutInSeconds = 10,
            RequiresUpn = false,
            EnableTrustedDomains = true,
            EnableAlternativeSuffixes = true,
            DomainPermissions = rules,
            SuffixesPermissions = rules
        };
        
        config.Initialize(request);
        Assert.True(config.PhoneAttributes.SequenceEqual(request.PhoneAttributes));
        Assert.True(config.AccessGroups.SequenceEqual(request.AccessGroups));
        Assert.True(config.SecondFaGroups.SequenceEqual(request.SecondFaGroups));
        Assert.True(config.SecondFaBypassGroups.SequenceEqual(request.SecondFaBypassGroups));
        Assert.True(config.NestedGroupsBaseDns.SequenceEqual(request.NestedGroupsBaseDns));
        Assert.Equal(request.IdentityAttribute, config.IdentityAttribute);
        Assert.Equal(request.LoadNestedGroups, config.LoadNestedGroups);
        Assert.Equal(request.BindTimeoutInSeconds, config.BindTimeoutInSeconds);
        Assert.Equal(request.RequiresUpn, config.UpnRequired);
        Assert.Equal(request.EnableTrustedDomains, config.TrustedDomainsEnabled);
        Assert.Equal(request.EnableAlternativeSuffixes, config.AlternativeSuffixesEnabled);
        Assert.Equal(request.DomainPermissions, config.DomainPermissions);
        Assert.Equal(request.SuffixesPermissions, config.SuffixesPermissions);
    }

    private LdapServerConfiguration GetDefaultConfiguration() => new(
        "connection",
        "user",
        "password");
}