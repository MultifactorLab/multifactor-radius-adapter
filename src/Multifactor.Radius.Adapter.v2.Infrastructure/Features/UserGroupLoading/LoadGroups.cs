using System.DirectoryServices.Protocols;
using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Connection.LdapConnectionFactory;
using Multifactor.Core.Ldap.LdapGroup.Load;
using Multifactor.Radius.Adapter.v2.Application.Features.UserGroupLoading.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.UserGroupLoading.Ports;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.UserGroupLoading;

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
            new LdapConnectionString(dto.ConnectionString, true, false), 
            AuthType.Basic, 
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