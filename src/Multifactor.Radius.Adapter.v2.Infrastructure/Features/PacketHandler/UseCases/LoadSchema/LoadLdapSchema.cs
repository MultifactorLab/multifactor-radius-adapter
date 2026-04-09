using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Dto;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadSchema.Ports;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.UseCases.LoadSchema;

internal sealed class LoadLdapSchema : ILoadLdapSchema
{
    private readonly LdapSchemaLoader _schemaLoader;

    public LoadLdapSchema(LdapSchemaLoader schemaLoader)
    {
        _schemaLoader = schemaLoader ?? throw new ArgumentNullException(nameof(schemaLoader));
    }
    public ILdapSchema? Execute(LoadLdapSchemaDto dto)
    {
        var options = new LdapConnectionOptions(
            new LdapConnectionString(dto.ConnectionString),
            dto.AuthType,
            dto.UserName,
            dto.Password,
            TimeSpan.FromSeconds(dto.BindTimeoutInSeconds)
        );
        return _schemaLoader.Load(options);
    }
}