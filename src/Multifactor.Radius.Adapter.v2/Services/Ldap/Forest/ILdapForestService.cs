using Multifactor.Core.Ldap.Connection;
using Multifactor.Radius.Adapter.v2.Core.Ldap.Forest;

namespace Multifactor.Radius.Adapter.v2.Services.Ldap.Forest;

public interface ILdapForestService
{
    IReadOnlyCollection<LdapForestEntry> LoadLdapForest(LdapConnectionOptions connectionOptions, bool loadTrustedDomains, bool loadSuffixes);
}