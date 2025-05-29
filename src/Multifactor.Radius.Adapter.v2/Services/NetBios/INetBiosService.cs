using Multifactor.Core.Ldap.Name;
using Multifactor.Radius.Adapter.v2.Core.Ldap.Identity;

namespace Multifactor.Radius.Adapter.v2.Services.NetBios;

public interface INetBiosService
{
    string ConvertNetBiosToUpn(string clientKey, UserIdentity identity, DistinguishedName domain);
    DistinguishedName GetDomainByIdentityAsync(string clientKey, DistinguishedName domain, UserIdentity identity);
}