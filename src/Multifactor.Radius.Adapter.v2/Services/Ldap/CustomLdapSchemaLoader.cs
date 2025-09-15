using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Schema;

namespace Multifactor.Radius.Adapter.v2.Services.Ldap;

public class CustomLdapSchemaLoader : ILdapSchemaLoader
{
    private readonly ILdapSchemeLoaderWrapper _ldapSchemaLoader;
    private readonly ILogger<ILdapSchemaLoader> _logger;

    public CustomLdapSchemaLoader(
        ILdapSchemeLoaderWrapper ldapSchemaLoader,
        ILogger<ILdapSchemaLoader> logger)
    {
        ArgumentNullException.ThrowIfNull(ldapSchemaLoader, nameof(ldapSchemaLoader));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));
        
        _ldapSchemaLoader = ldapSchemaLoader;
        _logger = logger;
    }

    public ILdapSchema? Load(LdapConnectionOptions connectionOptions)
    {
        ILdapSchema? schema = null;
        try
        {
            schema = _ldapSchemaLoader.Load(connectionOptions);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error during loading LDAP schema.");
        }
        
        if (schema is null)
        {
            _logger.LogWarning("Failed to load LDAP schema of '{url}'", connectionOptions.ConnectionString.Host);
            return schema;
        }

        _logger.LogDebug("Successfully loaded LDAP schema of '{url}'", connectionOptions.ConnectionString.Host);
        return schema;
    }
}