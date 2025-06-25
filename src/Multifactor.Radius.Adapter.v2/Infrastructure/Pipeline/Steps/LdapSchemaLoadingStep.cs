using System.DirectoryServices.Protocols;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;
using Multifactor.Radius.Adapter.v2.Services.Ldap;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Steps;

public class LdapSchemaLoadingStep: IRadiusPipelineStep
{
    private readonly ILdapSchemaLoader _ldapSchemaLoader;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<LdapSchemaLoadingStep> _logger;
    
    public LdapSchemaLoadingStep(ILdapSchemaLoader ldapSchemaLoader, IMemoryCache memoryCache, ILogger<LdapSchemaLoadingStep> logger)
    {
        _ldapSchemaLoader = ldapSchemaLoader;
        _memoryCache = memoryCache;
        _logger = logger;
    }
    
    public Task ExecuteAsync(IRadiusPipelineExecutionContext context)
    {
        _logger.LogDebug("'{name}' started", nameof(LdapSchemaLoadingStep));
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        var schema = TryGetLdapSchema(context);

        if (schema is null)
        {
            _logger.LogWarning("Unable to load LDAP schema for '{domain}'", context.Settings.LdapServerConfiguration.ConnectionString);
            throw new InvalidOperationException();
        }

        context.LdapSchema = schema;
        return Task.CompletedTask;
    }

    private ILdapSchema? TryGetLdapSchema(IRadiusPipelineExecutionContext context)
    {
        var cacheKey = context.Settings.LdapServerConfiguration.ConnectionString;
        if (_memoryCache.TryGetValue(cacheKey, out ILdapSchema? schema))
        {
            _logger.LogDebug("Loaded LDAP schema for '{domain}' from cache.", cacheKey);
            return schema;
        }

        var options = GetLdapConnectionOptions(context.Settings.LdapServerConfiguration);
        schema = _ldapSchemaLoader.Load(options);

        if (schema is null)
            return schema;
          
        var expirationDate = DateTimeOffset.Now.AddHours(context.Settings.LdapServerConfiguration.LdapSchemaCacheLifeTimeInHours);  
        SaveToCache(cacheKey, schema, expirationDate);
        
        _logger.LogDebug("LDAP schema for '{domain}' is saved in cache till '{expirationDate}'.", cacheKey, expirationDate.ToString());
        return schema;
    }

    private LdapConnectionOptions GetLdapConnectionOptions(ILdapServerConfiguration serverConfiguration)
    {
        return new LdapConnectionOptions(
            new LdapConnectionString(serverConfiguration.ConnectionString),
            AuthType.Basic,
            serverConfiguration.UserName,
            serverConfiguration.Password,
            TimeSpan.FromSeconds(serverConfiguration.BindTimeoutInSeconds));
    }

    private void SaveToCache(string cacheKey, ILdapSchema schema, DateTimeOffset expirationDate)
    {
        _memoryCache.Set(cacheKey, schema, expirationDate);
    }
}