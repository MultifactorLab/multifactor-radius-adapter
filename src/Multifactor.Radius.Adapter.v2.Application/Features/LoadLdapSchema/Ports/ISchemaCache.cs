using Multifactor.Core.Ldap.Schema;

namespace Multifactor.Radius.Adapter.v2.Application.Features.LoadLdapSchema.Ports;

public interface ISchemaCache
{
    void Set(string key, ILdapSchema value, DateTimeOffset expirationDate);
    bool TryGetValue(string key, out ILdapSchema? value);
}