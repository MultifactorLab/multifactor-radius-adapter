using System.DirectoryServices.Protocols;
using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Connection.LdapConnectionFactory;
using Multifactor.Core.Ldap.Extensions;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Abstractions;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadProfile.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadProfile.Ports;
using Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.UseCases.LoadProfile.Models;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.UseCases.LoadProfile;

internal sealed class LdapProfileSearch : IProfileSearch
{
    private readonly ILogger<IProfileSearch> _logger;
    private readonly ILdapConnectionFactory _connectionFactory;
    public LdapProfileSearch(ILogger<IProfileSearch> logger,
        ILdapConnectionFactory connectionFactory)
    {
        _logger = logger;
        _connectionFactory = connectionFactory;
    }
    
    public ILdapProfile? Execute(FindUserDto dto)
    {
        _logger.LogDebug("Try to find '{userIdentity}' profile at '{domain}'.", dto.UserIdentity.Identity, dto.SearchBase.StringRepresentation);

        var identityToSearch = dto.UserIdentity;
        if (dto.UserIdentity.Format == UserIdentityFormat.NetBiosName)
        {
            var index = dto.UserIdentity.Identity.IndexOf('\\');
            if (index <= 0)
                throw new ArgumentException($"Invalid NetBIOS identity: {dto.UserIdentity.Identity}");
            var userName = dto.UserIdentity.Identity[(index + 1)..];
            identityToSearch = new UserIdentity(userName);
        }
        var filter = GetFilter(identityToSearch, dto.LdapSchema);
        _logger.LogDebug("Search base = '{searchBase}'. Filter for search = '{filter}'", dto.SearchBase.StringRepresentation, filter);
        
        var connectionString = new LdapConnectionString(dto.ConnectionString);
        var options = new LdapConnectionOptions(connectionString,
            dto.AuthType,
            dto.UserName,
            dto.Password,
            TimeSpan.FromSeconds(dto.BindTimeoutInSeconds));
        using var connection = _connectionFactory.CreateConnection(options);
        var entry = connection.FindOne(dto.SearchBase, filter, SearchScope.Subtree, attributes: dto.AttributeNames ?? []);

        return entry is null ? null : new LdapProfile(entry, dto.LdapSchema);
    }

    private static string GetFilter(UserIdentity identity, ILdapSchema schema)
    {
        var identityAttribute = GetIdentityAttribute(identity, schema);
        var objectClass = schema.ObjectClass;
        var classValue = schema.UserObjectClass;

        return $"(&({objectClass}={classValue})({identityAttribute}={identity.Identity}))";
    }

    private static string GetIdentityAttribute(UserIdentity identity, ILdapSchema schema) => identity.Format switch
    {
        UserIdentityFormat.UserPrincipalName => "userPrincipalName",
        UserIdentityFormat.DistinguishedName => schema.Dn,
        UserIdentityFormat.SamAccountName => schema.Uid,
        _ => throw new NotSupportedException("Unsupported user identity format")
    };
}