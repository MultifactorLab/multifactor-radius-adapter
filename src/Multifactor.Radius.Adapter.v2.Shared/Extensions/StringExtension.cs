using System.Text;

namespace Multifactor.Radius.Adapter.v2.Shared.Extensions;

public static class StringExtension
{
    public static string[] CustomSplit(this string? target, string separator = ";")
    {
        return target?
            .Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }
    
    public static string CanonicalizeUserName(this string userName)
    {
        if (string.IsNullOrEmpty(userName))
        {
            throw new ArgumentNullException(nameof(userName));
        }

        var identity = userName.ToLower();
        var index = identity.IndexOf('\\', StringComparison.Ordinal);
        if (index > 0)
        {
            identity = identity[(index + 1)..];
        }

        index = identity.IndexOf('@', StringComparison.Ordinal);
        if (index > 0)
        {
            identity = identity[..index];
        }

        return identity;
    }

    /// <summary>
    /// Check if username does not contains domain prefix or suffix
    /// </summary>
    public static bool IsCanonicalUserName(this string userName)
    {
        if (string.IsNullOrEmpty(userName))
        {
            throw new ArgumentNullException(nameof(userName));
        }

        return userName.IndexOfAny(new[] { '\\', '@' }) == -1;
    }
    
    public static byte[] ToByteArray(this string hex)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hex);
        
        var bytes = new byte[hex.Length / 2];
        for (int i = 0; i < hex.Length; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        }

        return bytes;
    }
    
    public static string FromBase64ToUtf8(this string st)
    {
        return Encoding.UTF8.GetString(Convert.FromBase64String(st));
    }
}