using System.Diagnostics;
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
    private const string ConfigurationNamingContextAttribute = "configurationNamingContext";
    private const string NetBiosNameAttribute = "nETBIOSName";
    private const string DnsRootAttribute = "dnsRoot";

    private readonly ILdapConnectionFactory _connectionFactory;
    private readonly ILogger<IProfileSearch> _logger;

    public LdapProfileSearch(ILdapConnectionFactory connectionFactory, ILogger<IProfileSearch> logger)
    {
        _logger = logger;
        _connectionFactory = connectionFactory;
    }
    
    public FindUserResult Execute(FindUserDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var connectionString = new LdapConnectionString(dto.ConnectionString);

        return connectionString.IsGlobalCatalog
            ? ExecuteViaGlobalCatalog(dto, connectionString)
            : ExecuteSingleDomain(dto);
    }

    private FindUserResult ExecuteSingleDomain(FindUserDto dto)
    {
        _logger.LogDebug("Try to find '{userIdentity}' profile at '{domain}'.", dto.UserIdentity.Identity, dto.SearchBase.StringRepresentation);

        var filter = BuildFilter(dto);
        _logger.LogDebug("Search base = '{searchBase:l}'. Filter for search = '{filter:l}'", dto.SearchBase.StringRepresentation, filter);

        using var connection = CreateConnection(dto);
        var entry = connection.FindOne(dto.SearchBase, filter, SearchScope.Subtree, attributes: dto.AttributeNames ?? []);

        if (entry is null)
        {
            return new FindUserResult.NotFound(IsFinal: false);
        }

        _logger.LogDebug("'{userIdentity:l}' profile at '{domain:l}' was found.", dto.UserIdentity.Identity, dto.SearchBase.StringRepresentation);
        var profile = new LdapProfile(entry, dto.LdapSchema);
        return new FindUserResult.Found(profile, dto.ConnectionString);
    }

    /// <summary>
    /// Один запрос ко всему лесу. Из найденного DN вычисляется домен пользователя
    /// и connection-string для bind напрямую к контроллеру этого домена — на Global Catalog bind не делается.
    /// </summary>
    private FindUserResult ExecuteViaGlobalCatalog(FindUserDto dto, LdapConnectionString globalCatalogConnectionString)
    {
        var stopwatch = Stopwatch.StartNew();
        var filter = BuildFilter(dto);

        using var connection = CreateConnection(dto);
        var entries = connection.Find(DistinguishedName.Empty, filter, SearchScope.Subtree, attributes: dto.AttributeNames ?? []);
        stopwatch.Stop();

        var matches = entries.Select(entry => (ILdapProfile)new LdapProfile(entry, dto.LdapSchema)).ToList();

        _logger.LogInformation(
            "Global Catalog search for '{UserIdentity:l}' took {ElapsedMs} ms. Matches: {Count}",
            dto.UserIdentity.Identity, stopwatch.ElapsedMilliseconds, matches.Count);

        // Если пользователь явно указал домен (DOMAIN\user), результат обязан принадлежать
        // именно этому домену, независимо от того, сколько совпадений вернул GC.
        if (dto.UserIdentity.Format == UserIdentityFormat.NetBiosName)
        {
            matches = FilterByNetBiosDomain(dto, matches);
        }

        var profile = ResolveSingleMatch(dto.UserIdentity, matches);
        if (profile is null)
        {
            return new FindUserResult.NotFound(IsFinal: true);
        }

        var domainDnsName = profile.Dn.GetDomainDnsName();
        var bindConnectionString = globalCatalogConnectionString.ToDomainController(domainDnsName);

        if (bindConnectionString.StartsWith("ldaps://", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Resolved bind target '{ConnectionString:l}' uses LDAPS. Domain controller certificates are usually " +
                "issued for the DC's own FQDN, not the bare domain DNS name '{Domain:l}'",
                bindConnectionString, domainDnsName);
        }

        _logger.LogDebug(
            "User '{UserIdentity:l}' resolved to domain '{Domain:l}' via Global Catalog. Bind target: '{ConnectionString:l}'",
            dto.UserIdentity.Identity, domainDnsName, bindConnectionString);

        var fullProfileStopwatch = Stopwatch.StartNew();
        var fullProfile = LoadFullProfileFromDomainController(dto, bindConnectionString, profile.Dn);
        fullProfileStopwatch.Stop();

        if (fullProfile is null)
        {
            _logger.LogWarning(
                "Could not re-fetch full profile for '{UserIdentity:l}' from '{ConnectionString:l}' in {ElapsedMs} ms. ",
                dto.UserIdentity.Identity, bindConnectionString, fullProfileStopwatch.ElapsedMilliseconds);
            return new FindUserResult.Found(profile, bindConnectionString);
        }

        _logger.LogInformation(
            "Re-fetched full profile for '{UserIdentity:l}' from '{ConnectionString:l}' in {ElapsedMs} ms (GC only returns a partial attribute set).",
            dto.UserIdentity.Identity, bindConnectionString, fullProfileStopwatch.ElapsedMilliseconds);

        return new FindUserResult.Found(fullProfile, bindConnectionString);
    }

    /// <summary>
    /// Перечитывает профиль напрямую с DC, которому принадлежит пользователь — по его точному DN.
    /// </summary>
    private ILdapProfile? LoadFullProfileFromDomainController(FindUserDto dto, string bindConnectionString, DistinguishedName userDn)
    {
        try
        {
            var refetchDto = dto with
            {
                ConnectionString = bindConnectionString,
                SearchBase = userDn,
                AuthType = AuthType.Basic
            };

            var filter = BuildFilter(refetchDto);
            using var connection = CreateConnection(refetchDto);
            var entry = connection.FindOne(userDn, filter, SearchScope.Subtree, attributes: dto.AttributeNames ?? []);

            return entry is null ? null : new LdapProfile(entry, dto.LdapSchema);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Error while re-fetching full profile for '{UserIdentity:l}' from '{ConnectionString:l}'.",
                dto.UserIdentity.Identity, bindConnectionString);
            return null;
        }
    }

    /// <summary>
    /// sAMAccountName уникален только в пределах домена, не всего леса — один и тот же логин
    /// может найтись сразу в нескольких доменах. Если так и есть, пробуем определить нужный
    /// домен по UPN. Не получилось — отказываем.
    /// </summary>
    private ILdapProfile? ResolveSingleMatch(UserIdentity userIdentity, List<ILdapProfile> matches)
    {
        if (matches.Count == 0)
            return null;

        if (matches.Count == 1)
            return matches[0];

        var conflictingDns = matches.Select(m => m.Dn.StringRepresentation).ToList();
        _logger.LogWarning(
            "User '{UserIdentity:l}' matched {Count} entries in Global Catalog, login is not unique across the forest: {Dns}",
            userIdentity.Identity, matches.Count, conflictingDns);

        var match = TryFindMatchByUpnSuffix(userIdentity, matches);
        if (match is not null)
        {
            _logger.LogInformation(
                "'{UserIdentity:l}' isn't unique, picked '{Dn:l}' by UPN domain.",
                userIdentity.Identity, match.Dn.StringRepresentation);
            return match;
        }

        _logger.LogError(
            "'{UserIdentity:l}' matches {Count} users in different domains. Matches: {Dns}",
            userIdentity.Identity, matches.Count, conflictingDns);

        return null;
    }

    private static ILdapProfile? TryFindMatchByUpnSuffix(UserIdentity userIdentity, List<ILdapProfile> matches)
    {
        if (userIdentity.Format != UserIdentityFormat.UserPrincipalName)
        {
            return null;
        }

        var suffix = userIdentity.GetUpnSuffix();
        if (string.IsNullOrWhiteSpace(suffix))
        {
            return null;
        }

        var candidates = matches
            .Where(m => IsSameOrSubDomain(m.Dn.GetDomainDnsName(), suffix))
            .ToList();

        return candidates.Count == 1 ? candidates[0] : null;
    }

    /// <summary>
    /// Резолвит NetBIOS-домен из логина в DNS-имя (через crossRef в Configuration NC) и
    /// отфильтровывает GC-совпадения, оставляя только те, что принадлежат именно этому
    /// домену. Если домен не резолвится — отказываем.
    /// </summary>
    private List<ILdapProfile> FilterByNetBiosDomain(FindUserDto dto, List<ILdapProfile> matches)
    {
        var userIdentity = dto.UserIdentity;
        var index = userIdentity.Identity.IndexOf('\\');
        if (index <= 0)
        {
            return matches;
        }

        var netBiosName = userIdentity.Identity[..index];

        string? domainDns;
        try
        {
            domainDns = ResolveDomainDnsNameByNetBiosName(dto, netBiosName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve NetBIOS domain '{NetBiosName:l}' to a DNS domain name.", netBiosName);
            domainDns = null;
        }

        if (string.IsNullOrWhiteSpace(domainDns))
        {
            _logger.LogWarning(
                "Could not resolve NetBIOS domain '{NetBiosName:l}' specified in login '{UserIdentity:l}'. " +
                "Treating as not found rather than searching without the domain the user explicitly specified.",
                netBiosName, userIdentity.Identity);
            return [];
        }

        var filtered = matches.Where(m => IsSameOrSubDomain(m.Dn.GetDomainDnsName(), domainDns)).ToList();

        if (filtered.Count != matches.Count)
        {
            _logger.LogInformation(
                "Filtered Global Catalog matches for '{UserIdentity:l}' by explicit NetBIOS domain '{NetBiosName:l}' ('{DomainDns:l}'): {Before} -> {After}.",
                userIdentity.Identity, netBiosName, domainDns, matches.Count, filtered.Count);
        }

        return filtered;
    }

    private static bool IsSameOrSubDomain(string domainDns, string expectedDomainDns) =>
        domainDns.Equals(expectedDomainDns, StringComparison.OrdinalIgnoreCase)
        || domainDns.EndsWith("." + expectedDomainDns, StringComparison.OrdinalIgnoreCase);

    private string? ResolveDomainDnsNameByNetBiosName(FindUserDto dto, string netBiosName)
    {
        using var connection = CreateConnection(dto);

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
                dto.ConnectionString, netBiosName);
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
}