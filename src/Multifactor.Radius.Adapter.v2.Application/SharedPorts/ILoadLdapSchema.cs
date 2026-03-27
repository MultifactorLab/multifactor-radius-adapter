using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Dto;

namespace Multifactor.Radius.Adapter.v2.Application.SharedPorts;

public interface ILoadLdapSchema
{
    ILdapSchema? Execute(LoadLdapSchemaDto request);
}