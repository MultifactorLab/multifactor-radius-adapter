using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.Exceptions;
using NetTools;

namespace Multifactor.Radius.Adapter.v2.Core.Configuration.Client;

public class LdapServerConfiguration : ILdapServerConfiguration
{
    private string? _identity;
    private bool _loadNestedGroups;
    private int _timeout;
    private bool _trustedDomainsEnabled;
    private bool _alternativeSuffixesEnabled;
    private bool _requiresUpn;
    private readonly List<string> _accessGroups = new List<string>();
    private readonly List<string> _2FaGroups = new List<string>();
    private readonly List<string> _2FaBypassGroups = new List<string>();
    private readonly List<string> _baseDns = new List<string>();
    private readonly List<string> _phones = new List<string>();
    private IPermissionRules _domainPermissionRules = new PermissionRules();
    private IPermissionRules _suffixesPermissionRules = new PermissionRules();
    private readonly List<IPAddressRange> _ipWhiteList = new();
    private readonly List<string> _authenticationCacheGroups = new();
    
    public string ConnectionString { get; }
    public string UserName { get; }
    public string Password { get; }
    public int BindTimeoutInSeconds => _timeout;
    public bool LoadNestedGroups => _loadNestedGroups;
    public string? IdentityAttribute => _identity;
    public IReadOnlyList<string> AccessGroups => _accessGroups;
    public IReadOnlyList<string> SecondFaGroups => _2FaGroups;
    public IReadOnlyList<string> SecondFaBypassGroups => _2FaBypassGroups;
    public IReadOnlyList<string> NestedGroupsBaseDns => _baseDns;
    public IReadOnlyList<string> PhoneAttributes => _phones;
    public IPermissionRules DomainPermissions => _domainPermissionRules;
    public IPermissionRules SuffixesPermissions => _suffixesPermissionRules;
    public int LdapSchemaCacheLifeTimeInHours { get; } = 1;
    public int UserProfileCacheLifeTimeInHours { get; } = 1;
    public bool TrustedDomainsEnabled => _trustedDomainsEnabled;
    public bool AlternativeSuffixesEnabled => _alternativeSuffixesEnabled;
    public bool UpnRequired => _requiresUpn;
    public IReadOnlyList<IPAddressRange> IpWhiteList => _ipWhiteList;
    public IReadOnlyList<string> AuthenticationCacheGroups => _authenticationCacheGroups;

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
            .AddAccessGroups(settings.AccessGroups)
            .AddSecondFaGroups(settings.SecondFaGroups)
            .AddSecondFaBypassGroups(settings.SecondFaBypassGroups)
            .AddNestedGroupBaseDns(settings.NestedGroupsBaseDns)
            .SetIdentityAttribute(settings.IdentityAttribute)
            .SetLoadNestedGroups(settings.LoadNestedGroups)
            .SetBindTimeoutInSeconds(settings.BindTimeoutInSeconds)
            .RequiresUpn(settings.RequiresUpn)
            .EnableTrustedDomains(settings.EnableTrustedDomains)
            .EnableAlternativeSuffixes(settings.EnableAlternativeSuffixes)
            .SetDomainRules(settings.DomainPermissions)
            .SetAlternativeSuffixesRules(settings.SuffixesPermissions);
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

    // TODO add to ldap server config this settings
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
        
        AddToList(_accessGroups, groups);
        return this;
    }
    
    public LdapServerConfiguration AddSecondFaGroups(IEnumerable<string> groups)
    {
        if (groups is null)
            return this;
        
        AddToList(_2FaGroups, groups);
        return this;
    }
    
    public LdapServerConfiguration AddSecondFaBypassGroups(IEnumerable<string> groups)
    {
        if (groups is null)
            return this;
        
        AddToList(_2FaBypassGroups, groups);
        return this;
    }
    
    public LdapServerConfiguration AddNestedGroupBaseDns(IEnumerable<string> items)
    {
        if (items is null)
            return this;
        
        AddToList(_baseDns, items);
        return this;
    }
    
    public LdapServerConfiguration AddPhoneAttributes(IEnumerable<string> items)
    {
        if (items is null)
            return this;
        
        AddToList(_phones, items);
        return this;
    }
    
    //maybe for future
    public LdapServerConfiguration AddWhiteIpList(IEnumerable<string> ranges)
    {
        if (ranges is null)
            return this;
        
        foreach (var range in ranges)
        {
            if (!IPAddressRange.TryParse(range, out var ipAddressRange))
                throw new InvalidConfigurationException($"Invalid IP address range: '{range}' config");
            
            AddToList(_ipWhiteList, [ipAddressRange]);
        }

        return this;
    }
    
    public LdapServerConfiguration AddAuthenticationCacheGroups(IEnumerable<string> groups)
    {
        if (groups?.Count() > 0)
            return AddToList(_authenticationCacheGroups, groups);
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