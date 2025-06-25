using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.Ldap;

namespace Multifactor.Radius.Adapter.v2.Services.Ldap;

public class ChangeUserPasswordRequest
{
    public string NewPassword { get; }
    public ILdapProfile Profile { get; }
    public ILdapServerConfiguration ServerConfiguration { get; }
    public ILdapSchema Schema { get; }

    public ChangeUserPasswordRequest(string newPassword, ILdapProfile profile, ILdapServerConfiguration configuration, ILdapSchema schema)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newPassword);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(schema);
        
        NewPassword = newPassword;
        Profile = profile;
        ServerConfiguration = configuration;
        Schema = schema;
    }
}