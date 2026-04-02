using Multifactor.Core.Ldap.Schema;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadSchema.Ports;

public interface ISchemaCache
{
    void Set(string key, ILdapSchema value);
    bool TryGetValue(string key, out ILdapSchema? value);
}