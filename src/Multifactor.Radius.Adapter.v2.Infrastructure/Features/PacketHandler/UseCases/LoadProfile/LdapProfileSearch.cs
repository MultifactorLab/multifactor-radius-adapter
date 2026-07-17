using System.DirectoryServices.Protocols;
using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Attributes;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Connection.LdapConnectionFactory;
using Multifactor.Core.Ldap.Extensions;
using Multifactor.Core.Ldap.Name;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Abstractions;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadProfile.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadProfile.Ports;
using Multifactor.Radius.Adapter.v2.Infrastructure.Extensions;
using Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.UseCases.LoadProfile.Models;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.UseCases.LoadProfile;

internal sealed class LdapProfileSearch : IProfileSearch
{
    private readonly ILdapConnectionFactory _connectionFactory;
    private readonly ILogger<IProfileSearch> _logger;
    public LdapProfileSearch(ILdapConnectionFactory connectionFactory, ILogger<IProfileSearch> logger)
    {
        _logger = logger;
        _connectionFactory = connectionFactory;
    }
    
    public ILdapProfile? Execute(FindUserDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        _logger.LogDebug("Try to find '{userIdentity}' profile at '{domain}'.", dto.UserIdentity.Identity, dto.SearchBase.StringRepresentation);

        var filter = BuildFilter(dto);
        _logger.LogDebug("Search base = '{searchBase:l}'. Filter for search = '{filter:l}'", dto.SearchBase.StringRepresentation, filter);
        using var connection = CreateConnection(dto);
        var entry = connection.FindOne(dto.SearchBase, filter, SearchScope.Subtree, attributes: dto.AttributeNames ?? []);

        return entry is null ? null : new LdapProfile(entry, dto.LdapSchema);
    }

    public IReadOnlyList<ILdapProfile> ExecuteMany(FindUserDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        _logger.LogDebug("Try to find all '{userIdentity:l}' matches at '{domain:l}'.", dto.UserIdentity.Identity, dto.SearchBase.StringRepresentation);

        var filter = BuildFilter(dto);
        _logger.LogDebug("Search base = '{searchBase:l}'. Filter for search = '{filter:l}'", dto.SearchBase.StringRepresentation, filter);
        using var connection = CreateConnection(dto);
        var entries = connection.Find(dto.SearchBase, filter, SearchScope.Subtree, attributes: dto.AttributeNames ?? []);

        return entries.Select(entry => (ILdapProfile)new LdapProfile(entry, dto.LdapSchema)).ToList();
    }

    private static string BuildFilter(FindUserDto dto)
    {
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
        return filter;
    }

    private ILdapConnection CreateConnection(FindUserDto dto)
    {
        var connectionString = new LdapConnectionString(dto.ConnectionString);
        var options = new LdapConnectionOptions(connectionString,
            dto.AuthType,
            dto.UserName,
            dto.Password,
            TimeSpan.FromSeconds(dto.BindTimeoutInSeconds));
        return _connectionFactory.CreateConnection(options);
    }
    
    private static string GetFilter(UserIdentity identity, ILdapSchema schema)
    {
        var identityAttribute = GetIdentityAttribute(identity, schema);
        var objectClass = schema.ObjectClass;
        var classValue = schema.UserObjectClass;

        return $"(&({objectClass}={classValue})({identityAttribute}={identity.Identity.EscapeCharacters()}))";
    }

    private static string GetIdentityAttribute(UserIdentity identity, ILdapSchema schema) => identity.Format switch
    {
        UserIdentityFormat.UserPrincipalName => "userPrincipalName",
        UserIdentityFormat.DistinguishedName => schema.Dn,
        UserIdentityFormat.SamAccountName => schema.Uid,
        _ => throw new NotSupportedException("Unsupported user identity format")
    };

    private const string ConfigurationNamingContextAttribute = "configurationNamingContext";
    private const string NetBiosNameAttribute = "nETBIOSName";
    private const string DnsRootAttribute = "dnsRoot";

    public string? ResolveDomainDnsNameByNetBiosName(
        string connectionString,
        AuthType authType,
        string userName,
        string password,
        int bindTimeoutInSeconds,
        string netBiosName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(netBiosName);

        var options = new LdapConnectionOptions(
            new LdapConnectionString(connectionString),
            authType,
            userName,
            password,
            TimeSpan.FromSeconds(bindTimeoutInSeconds));

        using var connection = _connectionFactory.CreateConnection(options);

        var rootDse = connection.FindOne(
            DistinguishedName.Empty,
            "(objectClass=*)",
            SearchScope.Base,
            attributes: [new LdapAttributeName(ConfigurationNamingContextAttribute)]);

        var configurationNamingContext = rootDse?.Attributes
            .SafeGetAttributeValues(new LdapAttributeName(ConfigurationNamingContextAttribute))
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(configurationNamingContext))
        {
            _logger.LogWarning(
                "Could not determine Configuration naming context from RootDSE at '{ConnectionString:l}' while resolving NetBIOS domain '{NetBiosName:l}'.",
                connectionString, netBiosName);
            return null;
        }

        var partitionsBase = new DistinguishedName($"CN=Partitions,{configurationNamingContext}");
        var filter = $"(&(objectClass=crossRef)({NetBiosNameAttribute}={netBiosName.EscapeCharacters()}))";

        var entry = connection.FindOne(
            partitionsBase,
            filter,
            SearchScope.OneLevel,
            attributes: [new LdapAttributeName(DnsRootAttribute)]);

        var dnsRoot = entry?.Attributes.SafeGetAttributeValues(new LdapAttributeName(DnsRootAttribute)).FirstOrDefault();

        if (string.IsNullOrWhiteSpace(dnsRoot))
        {
            _logger.LogWarning(
                "NetBIOS domain '{NetBiosName:l}' was not found among crossRef objects under '{PartitionsBase:l}'.",
                netBiosName, partitionsBase.StringRepresentation);
        }

        return dnsRoot;
    }
}