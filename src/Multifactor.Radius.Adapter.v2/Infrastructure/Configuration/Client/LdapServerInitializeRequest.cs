using Multifactor.Core.Ldap.Name;
using Multifactor.Radius.Adapter.v2.Domain;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.Client;

public class LdapServerInitializeRequest
{
    public IEnumerable<string> PhoneAttributes { get; set; } = [];
    public IEnumerable<DistinguishedName> AccessGroups { get; set; } = [];
    public IEnumerable<DistinguishedName> SecondFaGroups { get; set; } = [];
    public IEnumerable<DistinguishedName> SecondFaBypassGroups { get; set; } = [];
    public IEnumerable<DistinguishedName> NestedGroupsBaseDns { get; set; } = [];
    public IEnumerable<DistinguishedName> AuthenticationCacheGroups { get; set; } = [];
    public string? IdentityAttribute { get; set; } = string.Empty;
    public bool LoadNestedGroups { get; set; } = true;
    public int BindTimeoutInSeconds { get; set; } = 30;
    public bool RequiresUpn { get; set; } =  false;
    public bool EnableTrustedDomains { get; set; } = false;
    public bool EnableAlternativeSuffixes { get; set; } = false;
    public IPermissionRules DomainPermissions { get; set; } = new PermissionRules();
    public IPermissionRules SuffixesPermissions { get; set; } = new PermissionRules();

    public LdapServerInitializeRequest()
    {
    }

    public LdapServerInitializeRequest(ILdapServerConfiguration config)
    {
        PhoneAttributes = config.PhoneAttributes;
        AccessGroups = config.AccessGroups;
        SecondFaGroups = config.SecondFaGroups;
        SecondFaBypassGroups = config.SecondFaBypassGroups;
        NestedGroupsBaseDns = config.NestedGroupsBaseDns;
        IdentityAttribute = config.IdentityAttribute;
        LoadNestedGroups = config.LoadNestedGroups;
        BindTimeoutInSeconds = config.BindTimeoutInSeconds;
        RequiresUpn = config.UpnRequired;
        EnableTrustedDomains = config.TrustedDomainsEnabled;
        EnableAlternativeSuffixes = config.AlternativeSuffixesEnabled;
        DomainPermissions = config.DomainPermissions;
        SuffixesPermissions = config.SuffixesPermissions;
        AuthenticationCacheGroups = config.AuthenticationCacheGroups;
    }

    public LdapServerInitializeRequest(Domain.RadiusAdapter.Sections.LdapServer.LdapServerConfiguration config)
    {
        PhoneAttributes = Split(config.PhoneAttributes);
        AccessGroups = Split(config.AccessGroups).Select(x => new DistinguishedName(x));
        SecondFaGroups = Split(config.SecondFaGroups).Select(x => new DistinguishedName(x));
        SecondFaBypassGroups = Split(config.SecondFaBypassGroups).Select(x => new DistinguishedName(x));
        NestedGroupsBaseDns = Split(config.NestedGroupsBaseDn).Select(x => new DistinguishedName(x));
        AuthenticationCacheGroups = Split(config.AuthenticationCacheGroups).Select(x => new DistinguishedName(x));
        IdentityAttribute = config.IdentityAttribute;
        LoadNestedGroups = config.LoadNestedGroups;
        BindTimeoutInSeconds = config.BindTimeoutInSeconds;
        RequiresUpn = config.RequiresUpn;
        EnableTrustedDomains = config.EnableTrustedDomains;
        EnableAlternativeSuffixes = config.EnableAlternativeSuffixes;
        DomainPermissions = GetPermissionRules(
            Split(config.IncludedDomains).ToList(),
            Split(config.ExcludedDomains).ToList());
        SuffixesPermissions = GetPermissionRules(
            Split(config.IncludedSuffixes).ToList(),
            Split(config.ExcludedSuffixes).ToList());
    }
    
    private static IEnumerable<string> Split(string value) => Utils.SplitString(value.ToLower());
    private static PermissionRules GetPermissionRules(List<string> included, List<string> excluded) => new(included, excluded);
}