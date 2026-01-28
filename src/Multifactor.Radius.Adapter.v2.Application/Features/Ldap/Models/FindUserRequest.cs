using Multifactor.Core.Ldap.Attributes;
using Multifactor.Core.Ldap.Name;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Models;

public class FindUserRequest
{
    public LdapConnectionData ConnectionData { get; set; }
    public UserIdentity UserIdentity { get; set; }
    public DistinguishedName SearchBase { get; set; }
    public ILdapSchema LdapSchema { get; set; }
    public LdapAttributeName[]? AttributeNames { get; set; }
}