using System.Text;
using Multifactor.Radius.Adapter.v2.Domain.Ldap.Identity;

namespace Multifactor.Radius.Adapter.v2.Domain;

public static class Utils
{
    public static byte[] StringToByteArray(string hex)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hex);
        
        var bytes = new byte[hex.Length / 2];
        for (int i = 0; i < hex.Length; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        }

        return bytes;
    }

    public static string ToHexString(this byte[] bytes)
    {
        return bytes != null ? BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant() : null;
    }

    public static string ToBase64(this byte[] bytes)
    {
        return bytes != null ? Convert.ToBase64String(bytes) : null;
    }

    public static string FromBase64ToUtf8(this string base64String)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(base64String);
        return Encoding.UTF8.GetString(Convert.FromBase64String(base64String));
    }

    public static string CanonicalizeUserName(string userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
            return string.Empty;

        var identity = userName.ToLowerInvariant();

        var backslashIndex = identity.IndexOf('\\');
        if (backslashIndex > 0)
            identity = identity[(backslashIndex + 1)..];

        var atIndex = identity.IndexOf('@');
        if (atIndex > 0)
            identity = identity[..atIndex];

        return identity;
    }

    public static bool IsCanonicalUserName(string userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
            return true;

        return userName.IndexOfAny(['\\', '@']) == -1;
    }

    public static string[] SplitString(string target, string separator = ";")
    {
        return target?
            .Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    public static string GetUpnSuffix(UserIdentity userIdentity)
    {
        ArgumentNullException.ThrowIfNull(userIdentity);

        if (userIdentity.Format != UserIdentityFormat.UserPrincipalName)
            return string.Empty;

        var parts = userIdentity.Identity.Split('@', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length > 1 ? parts.Last() : string.Empty;
    }
}