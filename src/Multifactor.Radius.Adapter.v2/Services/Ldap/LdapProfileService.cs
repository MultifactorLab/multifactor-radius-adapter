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
    private readonly IForestMetadataCache _forestMetadataCache;
    private readonly ILogger _logger;

    public LdapProfileService(LdapConnectionFactory ldapConnectionFactory, IForestMetadataCache forestMetadataCache, ILogger logger)
    {
        Throw.IfNull(ldapConnectionFactory, nameof(ldapConnectionFactory));
        Throw.IfNull(forestMetadataCache, nameof(forestMetadataCache));
        Throw.IfNull(logger, nameof(logger));
        
        _ldapConnectionFactory = ldapConnectionFactory;
        _forestMetadataCache = forestMetadataCache;
        _logger = logger;
    }

    public ILdapProfile? FindUserProfile(string clientName, ILdapServerConfiguration serverConfiguration, DistinguishedName searchBase, UserIdentity userIdentity, LdapAttributeName[]? attributeNames = null)
    {
        Throw.IfNull(searchBase, nameof(searchBase));
        Throw.IfNull(userIdentity, nameof(userIdentity));

        var options = GetLdapConnectionOptions(serverConfiguration);

        using var connection = _ldapConnectionFactory.CreateConnection(options);

        var identityToSearch = userIdentity;
        if (userIdentity.Format == UserIdentityFormat.NetBiosName)
        {
            var netBiosService = new NetBiosService(_forestMetadataCache, connection, _logger, serverConfiguration.DomainPermissionRules);
            var upn = netBiosService.ConvertNetBiosToUpn(clientName, identityToSearch, searchBase);
            identityToSearch = new UserIdentity(upn);
        }

        var filter = GetFilter(identityToSearch, serverConfiguration);
        var loader = new LdapProfileLoader(searchBase, connection, serverConfiguration.LdapSchema);
        return loader.LoadLdapProfile(filter, attributeNames: attributeNames ?? []);
    }

    public Task<PasswordChangeResponse> ChangeUserPasswordAsync(string newPassword, ILdapProfile ldapProfile, ILdapServerConfiguration serverConfiguration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newPassword, nameof(newPassword));
        ArgumentNullException.ThrowIfNull(ldapProfile, nameof(ldapProfile));

        var options = GetLdapConnectionOptions(serverConfiguration);
        
        using var connection = _ldapConnectionFactory.CreateConnection(options);
        var passwordChanger = new LdapPasswordChanger(connection, serverConfiguration.LdapSchema);
        return passwordChanger.ChangeUserPasswordAsync(newPassword, ldapProfile);
    }

    private string GetFilter(UserIdentity identity, ILdapServerConfiguration serverConfiguration)
    {
        var identityAttribute = GetIdentityAttribute(identity, serverConfiguration);
        var objectClass = serverConfiguration.LdapSchema.ObjectClass;
        var classValue = serverConfiguration.LdapSchema.UserObjectClass;

        return $"(&({objectClass}={classValue})({identityAttribute}={identity.Identity}))";
    }

    private string GetIdentityAttribute(UserIdentity identity, ILdapServerConfiguration serverConfiguration) => identity.Format switch
    {
        UserIdentityFormat.UserPrincipalName => "userPrincipalName",
        UserIdentityFormat.DistinguishedName => serverConfiguration.LdapSchema.Dn,
        UserIdentityFormat.SamAccountName => serverConfiguration.LdapSchema.Uid,
        _ => throw new NotSupportedException("Unsupported user identity format")
    };

    private LdapConnectionOptions GetLdapConnectionOptions(ILdapServerConfiguration serverConfiguration)
    {
        return new LdapConnectionOptions(
            new LdapConnectionString(serverConfiguration.ConnectionString),
            AuthType.Basic,
            serverConfiguration.UserName,
            serverConfiguration.Password,
            TimeSpan.FromSeconds(serverConfiguration.BindTimeoutInSeconds));
    }
}