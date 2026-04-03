using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Dto;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadSchema.Ports;

public interface ILoadLdapSchema
{
    ILdapSchema? Execute(LoadLdapSchemaDto request);
}