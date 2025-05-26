using Multifactor.Core.Ldap.Name;
using Multifactor.Radius.Adapter.v2.Core.Ldap.Forest;

namespace Multifactor.Radius.Adapter.v2.Services.LdapForest;

public interface IForestMetadataCache
{
    IForestSchema? Get(string key, DistinguishedName targetDomain);
    void Add(string key, IForestSchema forestSchema);
}