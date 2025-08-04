using Multifactor.Core.Ldap.Schema;

namespace Multifactor.Radius.Adapter.v2.Services.Ldap.Forest;

public class LdapForestLoaderProvider: ILdapForestLoaderProvider
{
    private readonly IEnumerable<ILdapForestLoader> _trustedDomainsLoaders;
    
    public LdapForestLoaderProvider(IEnumerable<ILdapForestLoader> loaders)
    {
        _trustedDomainsLoaders = loaders;
    }

    public ILdapForestLoader? GetTrustedDomainsLoader(LdapImplementation ldapImplementation)
    {
        return _trustedDomainsLoaders.FirstOrDefault(x => x.LdapImplementation == ldapImplementation);
    }
}