using System.DirectoryServices.Protocols;
using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.Ldap;
using Multifactor.Radius.Adapter.v2.Core.Ldap.Identity;
using Multifactor.Radius.Adapter.v2.Services.NetBios;
using ILdapConnectionFactory = Multifactor.Radius.Adapter.v2.Core.Ldap.ILdapConnectionFactory;

namespace Multifactor.Radius.Adapter.v2.Services.Ldap;

public class LdapProfileService : ILdapProfileService
{
    private readonly ILdapConnectionFactory _ldapConnectionFactory;
    private readonly INetBiosService _netBiosService;
    private readonly ILogger _logger;

    public LdapProfileService(ILdapConnectionFactory ldapConnectionFactory, INetBiosService netBiosService, ILogger<LdapProfileService> logger)
    {
        _ldapConnectionFactory = ldapConnectionFactory;
        _netBiosService = netBiosService;
        _logger = logger;
    }

    public ILdapProfile? FindUserProfile(FindUserProfileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var options = GetLdapConnectionOptions(request.LdapServerConfiguration);

        using var connection = _ldapConnectionFactory.CreateConnection(options);

        var identityToSearch = request.UserIdentity;
        if (request.UserIdentity.Format == UserIdentityFormat.NetBiosName)
        {
            var upn = _netBiosService.ConvertNetBiosToUpn(new NetBiosRequest(request.ClientName, identityToSearch, request.SearchBase, connection, request.LdapServerConfiguration.DomainPermissionRules));
            _logger.LogDebug("Transformed '{netbios}' to '{upn}'", request.UserIdentity.Identity, upn);
            identityToSearch = new UserIdentity(upn);
        }

        var filter = GetFilter(identityToSearch, request.LdapSchema);
        var loader = new LdapProfileLoader(request.SearchBase, connection, request.LdapSchema);
        return loader.LoadLdapProfile(filter, attributeNames: request.AttributeNames ?? []);
    }

    public Task<PasswordChangeResponse> ChangeUserPasswordAsync(ChangeUserPasswordRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        
        var options = GetLdapConnectionOptions(request.ServerConfiguration);
        
        using var connection = _ldapConnectionFactory.CreateConnection(options);
        var passwordChanger = new LdapPasswordChanger(connection, request.Schema);
        return passwordChanger.ChangeUserPasswordAsync(request.NewPassword, request.Profile);
    }

    private string GetFilter(UserIdentity identity, ILdapSchema schema)
    {
        var identityAttribute = GetIdentityAttribute(identity, schema);
        var objectClass = schema.ObjectClass;
        var classValue = schema.UserObjectClass;

        return $"(&({objectClass}={classValue})({identityAttribute}={identity.Identity}))";
    }

    private string GetIdentityAttribute(UserIdentity identity, ILdapSchema schema) => identity.Format switch
    {
        UserIdentityFormat.UserPrincipalName => "userPrincipalName",
        UserIdentityFormat.DistinguishedName => schema.Dn,
        UserIdentityFormat.SamAccountName => schema.Uid,
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