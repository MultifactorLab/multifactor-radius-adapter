using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Name;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.Ldap.Identity;

namespace Multifactor.Radius.Adapter.v2.Services.NetBios;

public class NetBiosRequest
{
    public string ClientKey { get; }
    public UserIdentity UserIdentity { get; }
    public DistinguishedName Domain { get; }
    public ILdapConnection Connection { get; }
    public IDomainPermissionRules? DomainPermissionRules { get; }

    public NetBiosRequest(string clientKey, UserIdentity userIdentity, DistinguishedName domain, ILdapConnection connection, IDomainPermissionRules? domainPermissionRules = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientKey, nameof(clientKey));
        ArgumentNullException.ThrowIfNull(userIdentity, nameof(userIdentity));
        ArgumentNullException.ThrowIfNull(domain, nameof(domain));
        ArgumentNullException.ThrowIfNull(connection, nameof(connection));
        
        ClientKey = clientKey;
        UserIdentity = userIdentity;
        Domain = domain;
        Connection = connection;
        DomainPermissionRules = domainPermissionRules;
    }
}