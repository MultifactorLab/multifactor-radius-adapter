using Multifactor.Core.Ldap.Name;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Models;

public class MembershipRequest
{
    public string ConnectionString { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
    public int BindTimeoutInSeconds { get; set; }
    public ILdapSchema LdapSchema { get; set; }
    public DistinguishedName DistinguishedName  { get; set; }
    public DistinguishedName? SearchBase { get; set; }
    public DistinguishedName[] TargetGroups { get; set; }
    public DistinguishedName[] NestedGroupsBaseDns { get; set; }

    public static MembershipRequest FromContext(RadiusPipelineContext context, IReadOnlyList<DistinguishedName> groups)
    {
        if (context.LdapConfiguration?.AccessGroups.Count == 0)
            throw new ArgumentNullException();
        
        return new MembershipRequest
        {
            ConnectionString = context.LdapConfiguration.ConnectionString,
            UserName = context.LdapConfiguration.Username,
            Password = context.LdapConfiguration.Password,
            LdapSchema = context.LdapSchema,
            DistinguishedName = context.LdapProfile.Dn,
            BindTimeoutInSeconds = context.LdapConfiguration.BindTimeoutSeconds,
            TargetGroups = groups.ToArray(),
            NestedGroupsBaseDns = context.LdapConfiguration.NestedGroupsBaseDns.ToArray()
        };
    }
}