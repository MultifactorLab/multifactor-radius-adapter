using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Dto;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadSchema.Ports;
using Multifactor.Radius.Adapter.v2.Application.SharedPorts;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadSchema;

internal sealed class LoadLdapSchemaStep: IRadiusPipelineStep
{
    private readonly ILoadLdapSchema _loadLdapSchema;
    private readonly ISchemaCache _cache;
    private readonly ILogger<LoadLdapSchemaStep> _logger;
    private const string StepName = nameof(LoadLdapSchemaStep);
    public LoadLdapSchemaStep(ILoadLdapSchema loadLdapSchema, ISchemaCache cache, ILogger<LoadLdapSchemaStep> logger)
    {
        _loadLdapSchema = loadLdapSchema;
        _cache = cache;
        _logger = logger;
    }
    
    public Task ExecuteAsync(RadiusPipelineContext context)
    {
        _logger.LogDebug("'{name}' started", StepName);
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(context.LdapConfiguration, nameof(context.LdapConfiguration));
        if (context.ForestMetadata is not null)
        {
            _logger.LogDebug("Forest metadata provided. Step skipped");
            return Task.CompletedTask;
        }
        var schema = TryGetLdapSchema(context);

        if (schema is null)
        {
            _logger.LogWarning("Unable to load LDAP schema for '{domain}'", context.LdapConfiguration.ConnectionString);
            throw new InvalidOperationException($"Unable to load LDAP schema for '{context.LdapConfiguration.ConnectionString}");
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

        var request = new LoadLdapSchemaDto(
            context.LdapConfiguration.ConnectionString,
            context.LdapConfiguration.Username,
            context.LdapConfiguration.Password,
            context.LdapConfiguration.BindTimeoutSeconds);
        schema = _loadLdapSchema.Execute(request);

        if (schema is null)
            return schema;
          
        _cache.Set(cacheKey, schema);
        return schema;
    }
}