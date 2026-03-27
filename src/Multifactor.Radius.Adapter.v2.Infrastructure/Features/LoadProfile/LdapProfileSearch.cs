using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Connection.LdapConnectionFactory;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Application.Features.LoadProfile.Ports;
using System.DirectoryServices.Protocols;
using Multifactor.Core.Ldap.Extensions;
using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Abstractions;
using Multifactor.Radius.Adapter.v2.Application.Features.LoadProfile.Models;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.LoadProfile;

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
    
    public ILdapProfile? Execute(FindUserDto request)
    {
        _logger.LogInformation("Try to find '{userIdentity}' profile at '{domain}'.", request.UserIdentity.Identity, request.SearchBase.StringRepresentation);
        var connectionString = new LdapConnectionString(request.ConnectionString, true);
        var options = new LdapConnectionOptions(connectionString,
            AuthType.Negotiate,
            request.UserName,
            request.Password,
            TimeSpan.FromSeconds(request.BindTimeoutInSeconds));
        using var connection = _connectionFactory.CreateConnection(options);
        var identityToSearch = request.UserIdentity;
        if (request.UserIdentity.Format == UserIdentityFormat.NetBiosName)
        {
            var index = request.UserIdentity.Identity.IndexOf('\\');
            if (index <= 0)
                throw new ArgumentException($"Invalid NetBIOS identity: {request.UserIdentity.Identity}");
            var userName = request.UserIdentity.Identity[(index + 1)..];
            identityToSearch = new UserIdentity(userName);
        }
        var filter = GetFilter(identityToSearch, request.LdapSchema);
        _logger.LogDebug("Search base = '{searchBase}'. Filter for search = '{filter}'", request.SearchBase.StringRepresentation, filter);
        var result = connection.Find(request.SearchBase, filter, SearchScope.Subtree, attributes: request.AttributeNames ?? []);
        var entry = result.FirstOrDefault();
        return entry is null ? null : new LdapProfile(entry, request.LdapSchema);
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