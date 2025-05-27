using Multifactor.Core.Ldap.Schema;

namespace Multifactor.Radius.Adapter.v2.Core.Configuration.Client;

public interface ILdapServerConfiguration
{
    public string ConnectionString { get; }
    public string UserName { get; }
    public string Password { get; }
    public int BindTimeoutInSeconds { get; }
    public bool LoadNestedGroups { get; }
    public string? IdentityAttribute { get; }
    public IReadOnlyList<string> AccessGroups { get; }
    public IReadOnlyList<string> SecondFaGroups { get; }
    public IReadOnlyList<string> SecondFaBypassGroups { get; }
    public IReadOnlyList<string> NestedGroupsBaseDns { get; }
    public IReadOnlyList<string> PhoneAttributes { get; }
    public ILdapSchema LdapSchema { get; }
    public IDomainPermissionRules DomainPermissionRules { get; }
}