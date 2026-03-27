using System.DirectoryServices.Protocols;
using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Connection.LdapConnectionFactory;
using Multifactor.Radius.Adapter.v2.Application.Features.FirstFactor.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.FirstFactor.Ports;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.FirstFactor.Adapters;

internal sealed class CheckConnection : ICheckConnection
{
    private readonly ILdapConnectionFactory _connectionFactory;

    public CheckConnection(ILdapConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public bool Execute(CheckConnectionDto dto)
    {
        var options = new LdapConnectionOptions(new LdapConnectionString(dto.ConnectionString, true, false), 
            AuthType.Basic, 
            dto.UserName, 
            dto.Password, 
            TimeSpan.FromSeconds(dto.BindTimeoutInSeconds));
        using var connection = _connectionFactory.CreateConnection(options);
        return true; //true or exception
    }
}