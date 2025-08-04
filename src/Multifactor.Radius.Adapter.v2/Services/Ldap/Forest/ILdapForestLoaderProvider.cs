using Multifactor.Core.Ldap.Schema;

namespace Multifactor.Radius.Adapter.v2.Services.Ldap.Forest;

public interface ILdapForestLoaderProvider
{
    public ILdapForestLoader? GetTrustedDomainsLoader(LdapImplementation ldapImplementation);
}