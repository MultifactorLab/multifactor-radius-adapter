using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Connection.LdapConnectionFactory;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.FirstFactor.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.FirstFactor.Ports;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.UseCases.FirstFactor;

internal sealed class CheckConnection : ICheckConnection
{
    private readonly ILdapConnectionFactory _connectionFactory;

    public CheckConnection(ILdapConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public bool Execute(CheckConnectionDto dto)
    {
        var options = new LdapConnectionOptions(new LdapConnectionString(dto.ConnectionString), 
            dto.AuthType,
            dto.UserName, 
            dto.Password, 
            TimeSpan.FromSeconds(dto.BindTimeoutInSeconds));
        using var connection = _connectionFactory.CreateConnection(options);
        return true; //true or exception
    }
}