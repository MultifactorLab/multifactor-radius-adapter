using Multifactor.Core.Ldap.LangFeatures;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models.Enum;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;

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

    private UserIdentityFormat GetIdentityTypeByIdentity(string identity)
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
    
    public string GetUpnSuffix()
    {
        if (Format != UserIdentityFormat.UserPrincipalName)
            return string.Empty;

        var suffix = Identity.Split('@', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Last();
        return suffix;
    }
}