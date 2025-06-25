using Multifactor.Core.Ldap.Name;
namespace Multifactor.Radius.Adapter.v2.Services.NetBios;

public interface INetBiosService
{
    string ConvertNetBiosToUpn(NetBiosRequest request);
    DistinguishedName GetDomainByIdentityAsync(NetBiosRequest request);
}