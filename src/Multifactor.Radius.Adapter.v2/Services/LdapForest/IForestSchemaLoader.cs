using Multifactor.Core.Ldap.Name;
using Multifactor.Radius.Adapter.v2.Core.Ldap.Forest;

namespace Multifactor.Radius.Adapter.v2.Services.LdapForest;

public interface IForestSchemaLoader
{
    public IForestSchema Load(DistinguishedName root);
}