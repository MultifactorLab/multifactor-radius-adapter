namespace Multifactor.Radius.Adapter.v2.Core.Configuration.Client;

public class LdapServerConfiguration : ILdapServerConfiguration
{
    private string? _identity;
    private bool _loadNestedGroups;
    private int _timeout;
    private readonly List<string> _accessGroups = new List<string>();
    private readonly List<string> _2FaGroups = new List<string>();
    private readonly List<string> _2FaBypassGroups = new List<string>();
    private readonly List<string> _baseDns = new List<string>();
    private readonly List<string> _phones = new List<string>();

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

    public LdapServerConfiguration(string? connectionString, string? userName, string? password)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentNullException(nameof(connectionString));
        if (string.IsNullOrWhiteSpace(userName))
            throw new ArgumentNullException(nameof(userName));
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentNullException(nameof(password));

        ConnectionString = connectionString;
        UserName = userName;
        Password = password;
    }

    public LdapServerConfiguration SetBindTimeoutInSeconds(int seconds)
    {
        if (seconds < 0)
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

    private LdapServerConfiguration AddToList<T>(IList<T> target, params T[] items)
    {
        foreach (var group in items)
        {
            target.Add(group);
        }

        return this;
    }
}