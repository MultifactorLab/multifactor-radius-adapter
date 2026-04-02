using System.DirectoryServices.Protocols;
using Multifactor.Core.Ldap.Name;
using Multifactor.Core.Ldap.Schema;

namespace Multifactor.Radius.Adapter.v2.Application.Core.Models.Dto;

public sealed record MembershipDto
{
    public string ConnectionString { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
    public int BindTimeoutInSeconds { get; set; }
    public ILdapSchema LdapSchema { get; set; }
    public DistinguishedName DistinguishedName  { get; set; }
    public DistinguishedName[] TargetGroups { get; set; }
    public DistinguishedName[] NestedGroupsBaseDns { get; set; }
    public AuthType AuthType  { get; set; }

    public static MembershipDto FromContext(RadiusPipelineContext context, IReadOnlyList<DistinguishedName> groups)
    {
        if (groups.Count == 0)
            throw new ArgumentNullException();
        
        return new MembershipDto
        {
            ConnectionString = context.LdapConfiguration.ConnectionString,
            UserName = context.LdapConfiguration.Username,
            Password = context.LdapConfiguration.Password,
            BindTimeoutInSeconds = context.LdapConfiguration.BindTimeoutSeconds,
            LdapSchema = context.LdapSchema,
            DistinguishedName = context.LdapProfile.Dn,
            TargetGroups = groups.ToArray(),
            NestedGroupsBaseDns = context.LdapConfiguration.NestedGroupsBaseDns.ToArray()
        };
    }
}