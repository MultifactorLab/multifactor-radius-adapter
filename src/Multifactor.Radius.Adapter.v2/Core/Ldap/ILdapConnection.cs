using System.Collections.ObjectModel;
using System.DirectoryServices.Protocols;
using Multifactor.Core.Ldap.Attributes;
using Multifactor.Core.Ldap.Entry;
using Multifactor.Core.Ldap.Name;

namespace Multifactor.Radius.Adapter.v2.Core.Ldap;

public interface ILdapConnection : Multifactor.Core.Ldap.Connection.ILdapConnection
{
    ReadOnlyCollection<LdapEntry> Find(
        DistinguishedName searchBase,
        string filter,
        SearchScope scope,
        PageResultRequestControl? pageControl = null,
        params LdapAttributeName[] attributes);
}