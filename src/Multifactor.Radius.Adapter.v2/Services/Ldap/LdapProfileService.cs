using System.DirectoryServices.Protocols;
using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Attributes;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Connection.LdapConnectionFactory;
using Multifactor.Core.Ldap.LangFeatures;
using Multifactor.Core.Ldap.Name;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.Ldap;
using Multifactor.Radius.Adapter.v2.Core.Ldap.Identity;
using Multifactor.Radius.Adapter.v2.Services.LdapForest;
using Multifactor.Radius.Adapter.v2.Services.NetBios;

namespace Multifactor.Radius.Adapter.v2.Services.Ldap;

public class LdapProfileService : ILdapProfileService
{
    private readonly LdapConnectionFactory _ldapConnectionFactory;
    private readonly ILdapServerConfiguration _ldapServerConfiguration;
    private readonly IForestMetadataCache _forestMetadataCache;
    private readonly string _clientName;
    private readonly ILogger _logger;

    public LdapProfileService(string clientName, ILdapServerConfiguration serverConfiguration, LdapConnectionFactory ldapConnectionFactory, IForestMetadataCache forestMetadataCache, ILogger logger)
    {
        Throw.IfNullOrWhiteSpace(clientName, nameof(clientName));
        Throw.IfNull(serverConfiguration, nameof(serverConfiguration));
        Throw.IfNull(ldapConnectionFactory, nameof(ldapConnectionFactory));
        Throw.IfNull(forestMetadataCache, nameof(forestMetadataCache));
        Throw.IfNull(logger, nameof(logger));

        _clientName = clientName;
        _ldapConnectionFactory = ldapConnectionFactory;
        _ldapServerConfiguration = serverConfiguration;
        _forestMetadataCache = forestMetadataCache;
        _logger = logger;
    }

    public ILdapProfile? LoadLdapProfile(DistinguishedName domain, UserIdentity userIdentity, LdapAttributeName[]? attributeNames = null)
    {
        Throw.IfNull(domain, nameof(domain));
        Throw.IfNull(userIdentity, nameof(userIdentity));
        
        var options = new LdapConnectionOptions(
            new LdapConnectionString(_ldapServerConfiguration.ConnectionString),
            AuthType.Basic,
            _ldapServerConfiguration.UserName,
            _ldapServerConfiguration.Password,
            TimeSpan.FromSeconds(_ldapServerConfiguration.BindTimeoutInSeconds));

        using var connection = _ldapConnectionFactory.CreateConnection(options);

        var identityToSearch = userIdentity;
        if (userIdentity.Format == UserIdentityFormat.NetBiosName)
        {
            var netBiosService = new NetBiosService(_forestMetadataCache, connection, _logger, _ldapServerConfiguration.DomainPermissionRules);
            var upn = netBiosService.ConvertNetBiosToUpn(_clientName, identityToSearch, domain);
            identityToSearch = new UserIdentity(upn);
        }

        var filter = GetFilter(identityToSearch);
        var loader = new LdapProfileLoader(domain, connection, _ldapServerConfiguration.LdapSchema);
        var profile = loader.LoadLdapProfile(filter, attributeNames: attributeNames ?? []);
        return profile;
    }

    private string GetFilter(UserIdentity identity)
    {
        var identityAttribute = GetIdentityAttribute(identity);
        var objectClass = _ldapServerConfiguration.LdapSchema.ObjectClass;
        var classValue = _ldapServerConfiguration.LdapSchema.UserObjectClass;

        return $"(&({objectClass}={classValue})({identityAttribute}={identity.Identity}))";
    }

    private string GetIdentityAttribute(UserIdentity identity) => identity.Format switch
    {
        UserIdentityFormat.UserPrincipalName => "userPrincipalName",
        UserIdentityFormat.DistinguishedName => _ldapServerConfiguration.LdapSchema.Dn,
        UserIdentityFormat.SamAccountName => _ldapServerConfiguration.LdapSchema.Uid,
        _ => throw new NotSupportedException("Unsupported user identity format")
    };
}