using Multifactor.Core.Ldap.Name;
using Multifactor.Radius.Adapter.v2.Application.Core.Enum;

namespace Multifactor.Radius.Adapter.v2.Application.Core.Models;

public sealed class UserIdentity
{
    public string Identity { get; init; }
    public UserIdentityFormat Format { get; init; }

    public UserIdentity(string? identity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identity, nameof(identity));
        Identity = identity;
        Format = GetIdentityTypeByIdentity(identity);
    }

    private static UserIdentityFormat GetIdentityTypeByIdentity(string identity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identity, nameof(identity));
        
        var id = identity.ToLower();
        
        if (id.Contains('\\'))
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

    public static string TransformDnToUpn(string dn)
    {
        var distinguishedName = new DistinguishedName(dn);

        // Получаем samAccountName (CN или другой RDN)
        var samAccountName = distinguishedName.Components.Deepest.Value;

        // Собираем DNS суффикс из компонентов DC=
        var dnsSuffix = string.Join(".", distinguishedName.Components
            .Where(x => x.Type == RdnAttributeType.DC)
            .Reverse()
            .Select(x => x.Value));

        // Формируем UPN: samAccountName@dnsSuffix
        return $"{samAccountName}@{dnsSuffix}";
    }
}