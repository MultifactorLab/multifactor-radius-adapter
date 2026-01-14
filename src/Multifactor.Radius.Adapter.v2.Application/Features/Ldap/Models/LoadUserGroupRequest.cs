using Multifactor.Core.Ldap.Name;
using Multifactor.Core.Ldap.Schema;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Models;

public class LoadUserGroupRequest
{
    public string ConnectionString { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
    public int BindTimeoutInSeconds { get; set; }
    public ILdapSchema LdapSchema { get; set; }
    public DistinguishedName UserDN { get; set; }
    public DistinguishedName? SearchBase { get; set; }
    public int Limit { get; set; } = int.MaxValue;
}