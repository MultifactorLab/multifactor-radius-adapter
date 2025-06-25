using Multifactor.Core.Ldap.Attributes;
using Multifactor.Core.Ldap.Name;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.Ldap.Identity;

namespace Multifactor.Radius.Adapter.v2.Services.Ldap;

public class FindUserProfileRequest
{
    public string ClientName { get; }
    public ILdapServerConfiguration  LdapServerConfiguration { get; }
    public ILdapSchema LdapSchema { get; }
    public DistinguishedName SearchBase { get; }
    public UserIdentity UserIdentity { get; }
    public LdapAttributeName[]? AttributeNames { get; }

    public FindUserProfileRequest(string clientName, ILdapServerConfiguration configuration, ILdapSchema ldapSchema, DistinguishedName searchBase, UserIdentity userIdentity, LdapAttributeName[]? attributeNames = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientName);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(ldapSchema);
        ArgumentNullException.ThrowIfNull(searchBase);
        ArgumentNullException.ThrowIfNull(userIdentity);
        
        ClientName = clientName;
        LdapServerConfiguration = configuration;
        LdapSchema = ldapSchema;
        SearchBase = searchBase;
        UserIdentity = userIdentity;
        AttributeNames = attributeNames;
    }
}