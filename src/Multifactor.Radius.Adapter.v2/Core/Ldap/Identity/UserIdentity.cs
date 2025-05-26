using Multifactor.Core.Ldap.LangFeatures;

namespace Multifactor.Radius.Adapter.v2.Core.Ldap.Identity;

public class UserIdentity
{
    public string Identity { get; private set; }
    public UserIdentityFormat Format { get; private set; }

    public UserIdentity(string identity)
    {
        Throw.IfNullOrWhiteSpace(identity, nameof(identity));
        Identity = identity;
        Format = GetIdentityTypeByIdentity(identity);
    }
    
    public UserIdentity(string identity, UserIdentityFormat format)
    {
        Throw.IfNullOrWhiteSpace(identity, nameof(identity));
        Identity = identity;
        Format = format;
    }

    public UserIdentityFormat GetIdentityTypeByIdentity(string identity)
    {
        Throw.IfNullOrWhiteSpace(identity, nameof(identity));
        
        var id = identity.ToLower();
        
        if (id.Contains("\\"))
            return UserIdentityFormat.NetBiosName;

        if (id.Contains('='))
            return UserIdentityFormat.DistinguishedName;

        if (id.Contains('@'))
            return UserIdentityFormat.UserPrincipalName;
        
        return UserIdentityFormat.SamAccountName;
    }
}