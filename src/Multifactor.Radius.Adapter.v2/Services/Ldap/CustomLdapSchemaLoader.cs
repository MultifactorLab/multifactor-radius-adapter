using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Schema;

namespace Multifactor.Radius.Adapter.v2.Services.Ldap;

public class CustomLdapSchemaLoader : ILdapSchemaLoader
{
    private readonly LdapSchemaLoader _ldapSchemaLoader;
    private readonly ILogger<ILdapSchemaLoader> _logger;

    public CustomLdapSchemaLoader(LdapSchemaLoader ldapSchemaLoader, ILogger<ILdapSchemaLoader> logger)
    {
        ArgumentNullException.ThrowIfNull(ldapSchemaLoader, nameof(ldapSchemaLoader));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));
        
        _ldapSchemaLoader = ldapSchemaLoader;
        _logger = logger;
    }

    public ILdapSchema? Load(LdapConnectionOptions connectionOptions)
    {
        var schema = _ldapSchemaLoader.Load(connectionOptions);
        if (schema is null)
        {
            _logger.LogWarning("Failed to load ldap schema of '{url}'", connectionOptions.ConnectionString);
            return null;
        }
        _logger.LogDebug("Successfully loaded ldap schema of '{url}'", connectionOptions.ConnectionString);
        return schema;
    }
}