namespace Multifactor.Radius.Adapter.v2.Core.Configuration.Client;

public class LdapServerInitializeRequest
{
    public IEnumerable<string> PhoneAttributes { get; set; } = Array.Empty<string>();
    public IEnumerable<string> AccessGroups { get; set; } = Array.Empty<string>();
    public IEnumerable<string> SecondFaGroups { get; set; } = Array.Empty<string>();
    public IEnumerable<string> SecondFaBypassGroups { get; set; } = Array.Empty<string>();
    public IEnumerable<string> NestedGroupsBaseDns { get; set; } = Array.Empty<string>();
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
    }

    public LdapServerInitializeRequest(Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Sections.LdapServer.LdapServerConfiguration config)
    {
        PhoneAttributes = Split(config.PhoneAttributes);
        AccessGroups = Split(config.AccessGroups);
        SecondFaGroups = Split(config.SecondFaGroups);
        SecondFaBypassGroups = Split(config.SecondFaBypassGroups);
        NestedGroupsBaseDns = Split(config.NestedGroupsBaseDn);
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