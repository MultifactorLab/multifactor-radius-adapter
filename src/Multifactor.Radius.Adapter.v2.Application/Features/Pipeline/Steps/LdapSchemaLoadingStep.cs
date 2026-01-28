using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Application.Cache;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Ports;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps;

public class LdapSchemaLoadingStep: IRadiusPipelineStep
{
    private readonly ILdapAdapter _ldapAdapter;
    private readonly ICacheService _cache;
    private readonly ILogger<LdapSchemaLoadingStep> _logger;
    private const int LdapSchemaCacheLifeTimeInHours = 1;
    public LdapSchemaLoadingStep(ILdapAdapter ldapAdapter, ICacheService cache, ILogger<LdapSchemaLoadingStep> logger)
    {
        _ldapAdapter = ldapAdapter;
        _cache = cache;
        _logger = logger;
    }
    
    public Task ExecuteAsync(RadiusPipelineContext context)
    {
        _logger.LogDebug("'{name}' started", nameof(LdapSchemaLoadingStep));
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(context.LdapConfiguration, nameof(context));
        
        var schema = TryGetLdapSchema(context);

        if (schema is null)
        {
            _logger.LogWarning("Unable to load LDAP schema for '{domain}'", context.LdapConfiguration.ConnectionString);
            throw new InvalidOperationException();
        }

        context.LdapSchema = schema;
        return Task.CompletedTask;
    }

    private ILdapSchema? TryGetLdapSchema(RadiusPipelineContext context)
    {
        var cacheKey = context.LdapConfiguration!.ConnectionString;
        if (_cache.TryGetValue(cacheKey, out ILdapSchema? schema))
        {
            _logger.LogDebug("Loaded LDAP schema for '{domain}' from cache.", cacheKey);
            return schema;
        }

        var request = new LdapConnectionData
        {
            ConnectionString = context.LdapConfiguration.ConnectionString,
            UserName = context.LdapConfiguration.Username,
            Password = context.LdapConfiguration.Password,
            BindTimeoutInSeconds = context.LdapConfiguration.BindTimeoutSeconds
        };
        schema = _ldapAdapter.LoadSchema(request);

        if (schema is null)
            return schema;
          
        var expirationDate = DateTimeOffset.Now.AddHours(LdapSchemaCacheLifeTimeInHours);  
        _cache.Set(cacheKey, schema, expirationDate);
        
        _logger.LogDebug("LDAP schema for '{domain}' is saved in cache till '{expirationDate}'.", cacheKey, expirationDate.ToString());
        return schema;
    }
}