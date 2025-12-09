using Multifactor.Core.Ldap.Name;
using NetTools;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.Client;

public class LdapServerConfiguration : ILdapServerConfiguration
{
    private string? _identity;
    private bool _loadNestedGroups;
    private int _timeout;
    private bool _trustedDomainsEnabled;
    private bool _alternativeSuffixesEnabled;
    private bool _requiresUpn;
    private readonly List<DistinguishedName> _accessGroups = [];
    private readonly List<DistinguishedName> _2FaGroups = [];
    private readonly List<DistinguishedName> _2FaBypassGroups = [];
    private readonly List<DistinguishedName> _baseDns = [];
    private readonly List<string> _phones = [];
    private IPermissionRules _domainPermissionRules = new PermissionRules();
    private IPermissionRules _suffixesPermissionRules = new PermissionRules();
    private readonly List<IPAddressRange> _ipWhiteList = [];
    private readonly List<DistinguishedName> _authenticationCacheGroups = [];
    
    public string ConnectionString { get; }
    public string UserName { get; }
    public string Password { get; }
    public int BindTimeoutInSeconds => _timeout;
    public bool LoadNestedGroups => _loadNestedGroups;
    public string? IdentityAttribute => _identity;
    public IReadOnlyList<DistinguishedName> AccessGroups => _accessGroups;
    public IReadOnlyList<DistinguishedName> SecondFaGroups => _2FaGroups;
    public IReadOnlyList<DistinguishedName> SecondFaBypassGroups => _2FaBypassGroups;
    public IReadOnlyList<DistinguishedName> NestedGroupsBaseDns => _baseDns;
    public IReadOnlyList<string> PhoneAttributes => _phones;
    public IPermissionRules DomainPermissions => _domainPermissionRules;
    public IPermissionRules SuffixesPermissions => _suffixesPermissionRules;
    public int LdapSchemaCacheLifeTimeInHours { get; } = 1;
    public int UserProfileCacheLifeTimeInHours { get; } = 0;
    public bool TrustedDomainsEnabled => _trustedDomainsEnabled;
    public bool AlternativeSuffixesEnabled => _alternativeSuffixesEnabled;
    public bool UpnRequired => _requiresUpn;
    public IReadOnlyList<IPAddressRange> IpWhiteList => _ipWhiteList;
    public IReadOnlyList<DistinguishedName> AuthenticationCacheGroups => _authenticationCacheGroups;

    public LdapServerConfiguration(string connectionString, string userName, string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        ConnectionString = connectionString;
        UserName = userName;
        Password = password;
    }

    public void Initialize(LdapServerInitializeRequest settings)
    {
        AddPhoneAttributes(settings.PhoneAttributes)
            .AddAccessGroups(settings.AccessGroups.Select(x=> x.StringRepresentation))
            .AddSecondFaGroups(settings.SecondFaGroups.Select(x=> x.StringRepresentation))
            .AddSecondFaBypassGroups(settings.SecondFaBypassGroups.Select(x=> x.StringRepresentation))
            .AddNestedGroupBaseDns(settings.NestedGroupsBaseDns.Select(x=> x.StringRepresentation))
            .SetIdentityAttribute(settings.IdentityAttribute)
            .SetLoadNestedGroups(settings.LoadNestedGroups)
            .SetBindTimeoutInSeconds(settings.BindTimeoutInSeconds)
            .RequiresUpn(settings.RequiresUpn)
            .EnableTrustedDomains(settings.EnableTrustedDomains)
            .EnableAlternativeSuffixes(settings.EnableAlternativeSuffixes)
            .SetDomainRules(settings.DomainPermissions)
            .SetAlternativeSuffixesRules(settings.SuffixesPermissions)
            .AddAuthenticationCacheGroups(settings.AuthenticationCacheGroups.Select(x=> x.StringRepresentation));
    }

    public LdapServerConfiguration EnableTrustedDomains(bool enable = true)
    {
        _trustedDomainsEnabled = enable;
        return this;
    }
    
    public LdapServerConfiguration EnableAlternativeSuffixes(bool enable = true)
    {
        _alternativeSuffixesEnabled = enable;
        return this;
    }
    
    public LdapServerConfiguration RequiresUpn(bool requires = true)
    {
        _requiresUpn = requires;
        return this;
    }

    public LdapServerConfiguration SetDomainRules(IPermissionRules rules)
    {
        _domainPermissionRules = rules;
        return this;
    }
    
    public LdapServerConfiguration SetAlternativeSuffixesRules(IPermissionRules rules)
    {
        _suffixesPermissionRules = rules;
        return this;
    }

    public LdapServerConfiguration SetBindTimeoutInSeconds(int seconds)
    {
        if (seconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(seconds));

        _timeout = seconds;
        return this;
    }

    public LdapServerConfiguration SetLoadNestedGroups(bool shouldLoad)
    {
        _loadNestedGroups = shouldLoad;
        return this;
    }

    public LdapServerConfiguration SetIdentityAttribute(string? attributeName)
    {
        _identity = attributeName;
        return this;
    }
    
    public LdapServerConfiguration AddAccessGroups(IEnumerable<string> groups)
    {
        if (groups is null)
            return this;
        
        AddToList(_accessGroups, groups.Select(x => new DistinguishedName(x)));
        return this;
    }
    
    public LdapServerConfiguration AddSecondFaGroups(IEnumerable<string> groups)
    {
        if (groups is null)
            return this;
        
        AddToList(_2FaGroups, groups.Select(x => new DistinguishedName(x)));
        return this;
    }
    
    public LdapServerConfiguration AddSecondFaBypassGroups(IEnumerable<string> groups)
    {
        if (groups is null)
            return this;
        
        AddToList(_2FaBypassGroups, groups.Select(x => new DistinguishedName(x)));
        return this;
    }
    
    public LdapServerConfiguration AddNestedGroupBaseDns(IEnumerable<string> items)
    {
        if (items is null)
            return this;
        
        AddToList(_baseDns, items.Select(x => new DistinguishedName(x)));
        return this;
    }
    
    public LdapServerConfiguration AddPhoneAttributes(IEnumerable<string> items)
    {
        if (items is null)
            return this;
        
        AddToList(_phones, items);
        return this;
    }
    
    public LdapServerConfiguration AddAuthenticationCacheGroups(IEnumerable<string> groups)
    {
        if (groups != null)
            return AddToList(_authenticationCacheGroups, groups.Select(x => new DistinguishedName(x)));
        return this;
    }
    
    private LdapServerConfiguration AddToList<T>(IList<T> target, IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            if (!target.Contains(item))
                target.Add(item);
        }
        
        return this;
    }
}