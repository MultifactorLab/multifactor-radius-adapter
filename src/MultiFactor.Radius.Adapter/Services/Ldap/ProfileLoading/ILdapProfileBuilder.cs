using MultiFactor.Radius.Adapter.Core.Ldap;

namespace MultiFactor.Radius.Adapter.Services.Ldap.ProfileLoading
{
    public interface ILdapProfileBuilder
    {
        ILdapProfile Build();
        ILdapProfileBuilder AddMemberOf(string group);
        ILdapProfileBuilder AddLdapAttr(string attr, object value);
        ILdapProfileBuilder SetUpn(string upn);
        ILdapProfileBuilder SetIdentityAttribute(string identityAttribute);
        ILdapProfileBuilder SetDisplayName(string displayname);
        ILdapProfileBuilder SetEmail(string email);
        ILdapProfileBuilder SetPhone(string phone);
    }
}
