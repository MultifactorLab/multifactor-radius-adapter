using System.Security.Cryptography;
using System.Text;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Security;

public static class ProtectionService
{
    public static string Protect(string secret, string data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(data, nameof(data));

        var bytes = StringToBytes(data);
        if (OperatingSystem.IsWindows())
        {
            var additionalEntropy = StringToBytes(secret);
            return ToBase64(ProtectedData.Protect(bytes, additionalEntropy, DataProtectionScope.CurrentUser));
        }
        return ToBase64(bytes);
    }

    public static string Unprotect(string secret, string data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(data, nameof(data));
        
        var bytes = FromBase64(data);
        if (OperatingSystem.IsWindows())
        {
            var additionalEntropy = StringToBytes(secret);
            return BytesToString(ProtectedData.Unprotect(bytes, additionalEntropy, DataProtectionScope.CurrentUser));
        }
        return BytesToString(bytes);
    }
    
    private static byte[] StringToBytes(string s)
    {
        return Encoding.UTF8.GetBytes(s);
    }

    private static string BytesToString(byte[] b)
    {
        return Encoding.UTF8.GetString(b);
    }

    private static string ToBase64(byte[] data)
    {
        return Convert.ToBase64String(data);
    }

    private static byte[] FromBase64(string text)
    {
        return Convert.FromBase64String(text);
    }
}