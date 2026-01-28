using Multifactor.Core.Ldap.Name;
using Multifactor.Core.Ldap.Schema;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Models;

public class ChangeUserPasswordRequest
{
    public LdapConnectionData ConnectionData { get; set; }
    public ILdapSchema LdapSchema { get; set; }
    public DistinguishedName DistinguishedName  { get; set; }
    public string NewPassword { get; set; }
}