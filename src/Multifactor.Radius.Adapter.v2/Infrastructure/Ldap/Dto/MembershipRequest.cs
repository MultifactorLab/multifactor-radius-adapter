using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Name;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Ldap.Dto;

public class MembershipRequest
{
    public DistinguishedName UserDn { get; }
    public IReadOnlyCollection<DistinguishedName> ProfileGroups { get; }
    public bool LoadNestedGroups { get; }
    public IReadOnlyCollection<DistinguishedName> NestedGroupsBaseDns { get; }
    public LdapConnectionString ConnectionString { get; }
    public string UserName { get; }
    public string Password { get; }
    public TimeSpan Timeout { get; }
    public ILdapSchema LdapSchema { get; }
    public IReadOnlyCollection<DistinguishedName> TargetGroups { get; }

    public MembershipRequest(RadiusPipelineExecutionContext context, IReadOnlyCollection<DistinguishedName> targetGroups)
    {
        ArgumentNullException.ThrowIfNull(context.UserLdapProfile);
        ArgumentNullException.ThrowIfNull(context.LdapServerConfiguration);
        ArgumentNullException.ThrowIfNull(context.LdapSchema);
        ArgumentNullException.ThrowIfNull(targetGroups);
        
        UserDn = context.UserLdapProfile.Dn;
        ProfileGroups = context.UserLdapProfile.MemberOf;
        LoadNestedGroups = context.LdapServerConfiguration.LoadNestedGroups;
        NestedGroupsBaseDns = context.LdapServerConfiguration.NestedGroupsBaseDns;
        UserName = context.LdapServerConfiguration.UserName;
        Password = context.LdapServerConfiguration.Password;
        Timeout = TimeSpan.FromSeconds(context.LdapServerConfiguration.BindTimeoutInSeconds);
        ConnectionString = new LdapConnectionString(context.LdapServerConfiguration.ConnectionString);
        LdapSchema = context.LdapSchema;
        TargetGroups = targetGroups;
    }
}