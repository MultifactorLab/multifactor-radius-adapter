using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Connection.LdapConnectionFactory;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.FirstFactor.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.FirstFactor.Ports;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.UseCases.FirstFactor;

internal sealed class CheckConnection : ICheckConnection
{
    private readonly ILdapConnectionFactory _connectionFactory;
    private readonly ILogger<ICheckConnection> _logger;

    public CheckConnection(ILdapConnectionFactory connectionFactory, ILogger<ICheckConnection> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public bool Execute(CheckConnectionDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var options = new LdapConnectionOptions(new LdapConnectionString(dto.ConnectionString),
            dto.AuthType,
            dto.UserName,
            dto.Password,
            TimeSpan.FromSeconds(dto.BindTimeoutInSeconds));
        
        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var connection = _connectionFactory.CreateConnection(options);
            stopwatch.Stop();
            _logger.LogInformation(
                "LDAP bind for user '{user:l}' to '{ldapUri:l}' took {ElapsedMs} ms. Success: {Success}",
                dto.UserName, dto.ConnectionString, stopwatch.ElapsedMilliseconds, true);

            return true; //true or exception
        }
        catch
        {
            stopwatch.Stop();
            _logger.LogInformation(
                "LDAP bind for user '{user:l}' to '{ldapUri:l}' took {ElapsedMs} ms. Success: {Success}",
                dto.UserName, dto.ConnectionString, stopwatch.ElapsedMilliseconds, false);
            throw;
        }
    }
}