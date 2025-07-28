using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.Exceptions;
using NetTools;

namespace Multifactor.Radius.Adapter.v2.Core.Configuration.Client;

public class LdapServerConfiguration : ILdapServerConfiguration
{
    private string? _identity;
    private bool _loadNestedGroups;
    private int _timeout;
    private readonly List<string> _accessGroups = new();
    private readonly List<string> _2FaGroups = new();
    private readonly List<string> _2FaBypassGroups = new();
    private readonly List<string> _baseDns = new();
    private readonly List<string> _phones = new();
    private DomainPermissionRules? _domainPermissionRules;
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
    public IDomainPermissionRules? DomainPermissionRules => _domainPermissionRules;
    public IReadOnlyList<IPAddressRange> IpWhiteList => _ipWhiteList;
    public IReadOnlyList<string> AuthenticationCacheGroups => _authenticationCacheGroups;
    public int LdapSchemaCacheLifeTimeInHours { get; } = 1;
    public int UserProfileCacheLifeTimeInHours { get; } = 1;

    public LdapServerConfiguration(string connectionString, string userName, string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        ConnectionString = connectionString;
        UserName = userName;
        Password = password;
    }

    // TODO add to ldap server config this settings
    public LdapServerConfiguration SetDomainPermissionRules(DomainPermissionRules rules)
    {
        _domainPermissionRules = rules;
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

    public LdapServerConfiguration AddAccessGroups(params string[] groups)
    {
        if (groups?.Length > 0)
            return AddToList(_accessGroups, groups);
        return this;
    }

    public LdapServerConfiguration AddSecondFaGroups(params string[] groups)
    {
        if (groups?.Length > 0)
            return AddToList(_2FaGroups, groups);
        return this;
    }

    public LdapServerConfiguration AddSecondFaBypassGroups(params string[] groups)
    {
        if (groups?.Length > 0)
            return AddToList(_2FaBypassGroups, groups);
        return this;
    }

    public LdapServerConfiguration AddNestedGroupBaseDns(params string[] groups)
    {
        if (groups?.Length > 0)
            return AddToList(_baseDns, groups);
        return this;
    }

    public LdapServerConfiguration AddPhoneAttributes(params string[] groups)
    {
        if (groups?.Length > 0)
            return AddToList(_phones, groups);
        return this;
    }
    
    //maybe for future
    public LdapServerConfiguration AddWhiteIpList(params string[] ranges)
    {
        if (!(ranges?.Length > 0))
            return this;
        
        foreach (var range in ranges)
        {
            if (!IPAddressRange.TryParse(range, out var ipAddressRange))
                throw new InvalidConfigurationException($"Invalid IP address range: '{range}' config");
            
            AddToList(_ipWhiteList, ipAddressRange);
        }

        return this;
    }
    
    public LdapServerConfiguration AddAuthenticationCacheGroups(params string[] groups)
    {
        if (groups?.Length > 0)
            return AddToList(_authenticationCacheGroups, groups);
        return this;
    }

    private LdapServerConfiguration AddToList<T>(IList<T> target, params T[] items)
    {
        foreach (var group in items)
            target.Add(group);

        return this;
    }
}