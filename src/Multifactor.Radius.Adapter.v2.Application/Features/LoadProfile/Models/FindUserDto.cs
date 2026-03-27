using Multifactor.Core.Ldap.Attributes;
using Multifactor.Core.Ldap.Name;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.LoadProfile.Models;

public sealed record FindUserDto
{
    public string ConnectionString { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
    public int BindTimeoutInSeconds { get; set; }
    public UserIdentity UserIdentity { get; set; }
    public DistinguishedName SearchBase { get; set; }
    public ILdapSchema LdapSchema { get; set; }
    public LdapAttributeName[]? AttributeNames { get; set; }
}