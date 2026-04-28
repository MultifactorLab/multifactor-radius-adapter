using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Connection.LdapConnectionFactory;
using Multifactor.Core.Ldap.LdapGroup.Load;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadUserGroup.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadUserGroup.Ports;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.UseCases.LoadUserGroup;

internal sealed class LoadGroups : ILoadGroups
{
    private readonly ILdapConnectionFactory _connectionFactory;
    private readonly ILdapGroupLoaderFactory  _ldapGroupLoaderFactory;

    public LoadGroups(ILdapConnectionFactory connectionFactory, ILdapGroupLoaderFactory ldapGroupLoaderFactory)
    {
        _connectionFactory = connectionFactory;
        _ldapGroupLoaderFactory = ldapGroupLoaderFactory;
    }

    public IReadOnlyList<string> Execute(LoadUserGroupDto dto)
    {
        var options = new LdapConnectionOptions(
            new LdapConnectionString(dto.ConnectionString), 
            dto.AuthType,
            dto.UserName, 
            dto.Password, 
            TimeSpan.FromSeconds(dto.BindTimeoutInSeconds));
        using var connection = _connectionFactory.CreateConnection(options);        
        var groupLoader = _ldapGroupLoaderFactory.GetGroupLoader(dto.LdapSchema, connection, 
            dto.SearchBase ?? dto.LdapSchema.NamingContext);
        var groupDns = groupLoader.GetGroups(dto.UserDN, pageSize: 20);
        return groupDns.Take(dto.Limit).Select(x => x.Components.Deepest.Value).ToList();
    }
}