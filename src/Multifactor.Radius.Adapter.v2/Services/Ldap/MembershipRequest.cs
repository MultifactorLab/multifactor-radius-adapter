using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Name;
using Multifactor.Core.Ldap.Schema;

namespace Multifactor.Radius.Adapter.v2.Services.Ldap;

public class MembershipRequest
{
    public DistinguishedName UserDn { get; }
    public DistinguishedName[] ProfileGroups { get; }
    public bool LoadNestedGroups { get; }
    public DistinguishedName[] NestedGroupsBaseDns { get; }
    public LdapConnectionString ConnectionString { get; }
    public string UserName { get; }
    public string Password { get; }
    public TimeSpan Timeout { get; }
    public ILdapSchema LdapSchema { get; }
    public DistinguishedName[] TargetGroups { get; }

    public MembershipRequest(
        DistinguishedName userDn,
        DistinguishedName[] profileGroups,
        bool loadNestedGroups,
        DistinguishedName[] nestedGroupsBaseDns,
        string connectionString,
        string userName,
        string password,
        int timeoutInSeconds,
        ILdapSchema ldapSchema,
        DistinguishedName[] targetGroups)
    {
        ArgumentNullException.ThrowIfNull(userDn);
        ArgumentNullException.ThrowIfNull(profileGroups);
        ArgumentNullException.ThrowIfNull(nestedGroupsBaseDns);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(timeoutInSeconds);
        ArgumentNullException.ThrowIfNull(ldapSchema);
        ArgumentNullException.ThrowIfNull(targetGroups);
        
        UserDn = userDn;
        ProfileGroups = profileGroups;
        LoadNestedGroups = loadNestedGroups;
        NestedGroupsBaseDns = nestedGroupsBaseDns;
        ConnectionString = new LdapConnectionString(connectionString);
        UserName = userName;
        Password = password;
        Timeout = TimeSpan.FromSeconds(timeoutInSeconds);
        LdapSchema = ldapSchema;
        TargetGroups = targetGroups;
    }
}