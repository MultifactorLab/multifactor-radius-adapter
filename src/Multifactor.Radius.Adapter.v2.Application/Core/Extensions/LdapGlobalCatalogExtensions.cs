using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Name;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Abstractions;

namespace Multifactor.Radius.Adapter.v2.Application.Core.Extensions;

public static class LdapGlobalCatalogExtensions
{
    private const int GlobalCatalogPort = 3268;
    private const int GlobalCatalogSslPort = 3269;
    private const int LdapPort = 389;
    private const int LdapsPort = 636;

    /// <summary>
    /// LDAP-сервер считается Global Catalog, если в connection-string указан порт 3268/3269
    /// </summary>
    public static bool IsGlobalCatalog(this ILdapServerConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);
        return IsGlobalCatalogPort(new LdapConnectionString(config.ConnectionString).Port);
    }

    public static bool IsGlobalCatalogPort(int port) => port is GlobalCatalogPort or GlobalCatalogSslPort;

    /// <summary>
    /// Извлекает DNS-имя домена (например, "child.test.group") из DN пользователя,
    /// найденного через GC (например, "CN=User1,OU=Users,DC=child,DC=test,DC=group").
    /// </summary>
    public static string ExtractDomainDnsName(this DistinguishedName dn)
    {
        ArgumentNullException.ThrowIfNull(dn);

        var parts = dn.StringRepresentation
            .Split(',')
            .Select(p => p.Trim())
            .Where(p => p.StartsWith("DC=", StringComparison.OrdinalIgnoreCase))
            .Select(p => p[3..].Trim());

        return string.Join(".", parts);
    }

    /// <summary>
    /// Строит connection-string для bind к контроллеру конкретного домена по его DNS-имени.
    /// </summary>
    public static string ToDomainControllerConnectionString(this LdapConnectionString globalCatalogConnectionString, string domainDnsName)
    {
        ArgumentNullException.ThrowIfNull(globalCatalogConnectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(domainDnsName);

        var isSsl = globalCatalogConnectionString.Port == GlobalCatalogSslPort
            || globalCatalogConnectionString.Scheme.Equals("ldaps", StringComparison.OrdinalIgnoreCase);

        var scheme = isSsl ? "ldaps" : "ldap";
        var port = isSsl ? LdapsPort : LdapPort;

        return $"{scheme}://{domainDnsName}:{port}";
    }
}
