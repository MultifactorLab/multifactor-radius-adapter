using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Core.Ldap;
using Multifactor.Radius.Adapter.v2.Core.Ldap.Forest;
using Multifactor.Radius.Adapter.v2.Services.Cache;
using ILdapConnection = Multifactor.Radius.Adapter.v2.Core.Ldap.ILdapConnection;

namespace Multifactor.Radius.Adapter.v2.Services.Ldap.Forest;

public class LdapForestService : ILdapForestService
{
    private readonly ILdapSchemaLoader _ldapSchemaLoader;
    private readonly ILogger<LdapForestService> _logger;
    private readonly ILdapConnectionFactory _connectionFactory;
    private readonly ILdapForestLoaderProvider _ldapForestLoaderProvider;
    private readonly ICacheService _cache;
    
    public LdapForestService(
        ILdapSchemaLoader ldapSchemaLoader,
        ILdapConnectionFactory connectionFactory,
        ILdapForestLoaderProvider ldapForestLoaderProvider,
        ICacheService cache,
        ILogger<LdapForestService> logger)
    {
        _ldapSchemaLoader = ldapSchemaLoader;
        _connectionFactory = connectionFactory;
        _ldapForestLoaderProvider = ldapForestLoaderProvider;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Loads root and trusted domains
    /// </summary>
    public IReadOnlyCollection<LdapForestEntry> LoadLdapForest(LdapConnectionOptions connectionOptions, bool loadTrustedDomains, bool loadSuffixes)
    {
        var domain  = connectionOptions.ConnectionString.Host;
        var cacheKey = BuildCacheKey(domain);
        var forest = TryGetForestFromCache(cacheKey);
        if (forest != null)
        {
            _logger.LogDebug("Loaded LDAP forest for '{domain}' from cache.", domain);
            return forest;
        }

        forest = LoadForest(connectionOptions, loadTrustedDomains, loadSuffixes);
        var expirationDate = DateTimeOffset.Now.AddHours(1);  
        _cache.Set(cacheKey, forest, expirationDate);
        
        return forest;
    }

    private IReadOnlyCollection<LdapForestEntry> LoadForest(LdapConnectionOptions connectionOptions, bool loadTrustedDomains, bool loadSuffixes)
    {
        var domain  = connectionOptions.ConnectionString.Host;
        var mainSchema = LoadSchema(connectionOptions);

        if (mainSchema is null)
            return Array.Empty<LdapForestEntry>();
        
        var loader = GetForestLoader(mainSchema.LdapServerImplementation);
        
        if (loader is null)
        {
            _logger.LogDebug("Adapter does not support trusted domains feature for '{catalogType}' catalog '{domain}'. Loading is skipped", mainSchema.LdapServerImplementation, domain);
            return new List<LdapForestEntry>() { new(mainSchema, [LdapNamesUtils.DnToFqdn(mainSchema.NamingContext)])};
        }
        
        _logger.LogDebug("Loading forest schema from '{domain}'",  domain);
        using var connection = _connectionFactory.CreateConnection(connectionOptions);
       
        var schemas = new List<ILdapSchema> { mainSchema };
        if (loadTrustedDomains)
            schemas.AddRange(LoadTrustedSchemas(connection, loader, connectionOptions, mainSchema));
        else
            _logger.LogDebug("Trusted domains are not required for '{domain}'", domain);

        var forest = new List<LdapForestEntry>();
        
        foreach (var schema in schemas)
        {
            var fqdn = LdapNamesUtils.DnToFqdn(schema.NamingContext);
            var forestEntry = new LdapForestEntry(schema, [fqdn]);
            forest.Add(forestEntry);
            if (!loadSuffixes)
                continue;

            var suffixes = loader.LoadDomainSuffixes(connection, schema).ToList();

            if (suffixes.Any())
            {
                var str = string.Join(", ", suffixes);
                _logger.LogDebug("Loaded suffixes ({suffixes}) from '{domain}'", str, fqdn);
            }

            forestEntry.AddSuffix(suffixes);
        }
        
        return forest;
    }

    private IEnumerable<ILdapSchema> LoadTrustedSchemas(ILdapConnection connection, ILdapForestLoader loader, LdapConnectionOptions connectionOptions, ILdapSchema mainSchema)
    {
        var trustedDomains = loader.LoadTrustedDomains(connection, mainSchema);

        foreach (var trusted in trustedDomains)
        {
            var trustedFqdn = LdapNamesUtils.DnToFqdn(trusted);
            _logger.LogDebug("Found trusted domain: '{trustedDomain}'", trustedFqdn);
            var options = new LdapConnectionOptions(
                new LdapConnectionString(trustedFqdn),
                connectionOptions.AuthType,
                connectionOptions.Username,
                connectionOptions.Password,
                connectionOptions.Timeout);

            var trustedSchema = LoadSchema(options);
            if (trustedSchema is not null)
                yield return trustedSchema;
        }
    }

    private ILdapForestLoader? GetForestLoader(LdapImplementation ldapImplementation)
    {
        var loader = _ldapForestLoaderProvider.GetTrustedDomainsLoader(ldapImplementation);
        return loader;
    }

    private ILdapSchema? LoadSchema(LdapConnectionOptions connectionOptions)
    {
        var schema = _ldapSchemaLoader.Load(connectionOptions);
        return schema;
    }

    private IReadOnlyCollection<LdapForestEntry>? TryGetForestFromCache(string key)
    {
        _cache.TryGetValue(key, out IReadOnlyCollection<LdapForestEntry>? forest);
        return forest;
    }

    private string BuildCacheKey(string domain)
    {
        return "forest_" + domain;
    }
}