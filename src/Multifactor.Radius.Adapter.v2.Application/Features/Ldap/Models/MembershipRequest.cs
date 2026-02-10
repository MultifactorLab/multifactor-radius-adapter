using Multifactor.Core.Ldap.Name;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Models;

public class MembershipRequest
{
    public LdapConnectionData ConnectionData { get; set; }
    public ILdapSchema LdapSchema { get; set; }
    public DistinguishedName DistinguishedName  { get; set; }
    public DistinguishedName[] TargetGroups { get; set; }
    public DistinguishedName[] NestedGroupsBaseDns { get; set; }

    public static MembershipRequest FromContext(RadiusPipelineContext context, IReadOnlyList<DistinguishedName> groups)
    {
        if (groups.Count == 0)
            throw new ArgumentNullException();
        
        return new MembershipRequest
        {
            ConnectionData = new LdapConnectionData
            {
                ConnectionString = context.LdapConfiguration.ConnectionString,
                UserName = context.LdapConfiguration.Username,
                Password = context.LdapConfiguration.Password,
                BindTimeoutInSeconds = context.LdapConfiguration.BindTimeoutSeconds,
            },
            LdapSchema = context.LdapSchema,
            DistinguishedName = context.LdapProfile.Dn,
            TargetGroups = groups.ToArray(),
            NestedGroupsBaseDns = context.LdapConfiguration.NestedGroupsBaseDns.ToArray()
        };
    }
}