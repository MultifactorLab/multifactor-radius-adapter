using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Domain.Ldap.Interfaces;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.Client;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Ldap.Dto;

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