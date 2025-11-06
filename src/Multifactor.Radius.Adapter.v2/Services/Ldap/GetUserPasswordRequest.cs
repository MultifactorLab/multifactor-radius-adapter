using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.Ldap;

namespace Multifactor.Radius.Adapter.v2.Services.Ldap;

public class GetUserPasswordRequest
{
    public ILdapProfile Profile { get; }
    public ILdapServerConfiguration ServerConfiguration { get; }
    public ILdapSchema Schema { get; }

    public GetUserPasswordRequest(ILdapProfile profile, ILdapServerConfiguration configuration, ILdapSchema schema)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(schema);
        
        Profile = profile;
        ServerConfiguration = configuration;
        Schema = schema;
    }
}