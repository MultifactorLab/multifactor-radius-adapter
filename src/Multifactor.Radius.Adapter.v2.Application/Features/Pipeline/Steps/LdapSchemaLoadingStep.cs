using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Dto;
using Multifactor.Radius.Adapter.v2.Application.Features.LoadLdapSchema.Ports;
using Multifactor.Radius.Adapter.v2.Application.SharedPorts;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps;

internal sealed class LdapSchemaLoadingStep: IRadiusPipelineStep
{
    private readonly ILoadLdapSchema _loadLdapSchema;
    private readonly ISchemaCache _cache;
    private readonly ILogger<LdapSchemaLoadingStep> _logger;
    private const int LdapSchemaCacheLifeTimeInHours = 1; //TODO may to conf
    public LdapSchemaLoadingStep(ILoadLdapSchema loadLdapSchema, ISchemaCache cache, ILogger<LdapSchemaLoadingStep> logger)
    {
        _loadLdapSchema = loadLdapSchema;
        _cache = cache;
        _logger = logger;
    }
    
    public Task ExecuteAsync(RadiusPipelineContext context)
    {
        _logger.LogDebug("'{name}' started", nameof(LdapSchemaLoadingStep));
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(context.LdapConfiguration, nameof(context.LdapConfiguration));
        
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
        if (_cache.TryGetValue(cacheKey, out var schema))
        {
            _logger.LogDebug("Loaded LDAP schema for '{domain}' from cache.", cacheKey);
            return schema;
        }

        var request = new LoadLdapSchemaDto(context.LdapConfiguration.ConnectionString,
            context.LdapConfiguration.Username,
            context.LdapConfiguration.Password,
            context.LdapConfiguration.BindTimeoutSeconds);
        schema = _loadLdapSchema.Execute(request);

        if (schema is null)
            return schema;
          
        var expirationDate = DateTimeOffset.Now.AddHours(LdapSchemaCacheLifeTimeInHours);  
        _cache.Set(cacheKey, schema, expirationDate);
        
        _logger.LogDebug("LDAP schema for '{domain}' is saved in cache till '{expirationDate}'.", cacheKey, expirationDate.ToString());
        return schema;
    }
}