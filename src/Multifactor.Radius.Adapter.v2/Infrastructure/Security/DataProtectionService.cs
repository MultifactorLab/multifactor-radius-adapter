using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Security;

public static class DataProtectionService
{
    public static string Protect(string secret, string data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        ArgumentException.ThrowIfNullOrWhiteSpace(data);

        if (!IsWindows())
            return ToBase64(StringToBytes(data)); // Fallback for Linux

        var entropy = StringToBytes(secret);
        var protectedData = ProtectedData.Protect(
            StringToBytes(data),
            entropy,
            DataProtectionScope.CurrentUser);

        return ToBase64(protectedData);
    }

    public static string Unprotect(string secret, string data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        ArgumentException.ThrowIfNullOrWhiteSpace(data);

        if (!IsWindows())
            return BytesToString(FromBase64(data)); // Fallback for Linux

        var entropy = StringToBytes(secret);
        var unprotectedData = ProtectedData.Unprotect(
            FromBase64(data),
            entropy,
            DataProtectionScope.CurrentUser);

        return BytesToString(unprotectedData);
    }

    private static bool IsWindows()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }

    private static byte[] StringToBytes(string s) => Encoding.UTF8.GetBytes(s);
    private static string BytesToString(byte[] b) => Encoding.UTF8.GetString(b);
    private static string ToBase64(byte[] data) => Convert.ToBase64String(data);
    private static byte[] FromBase64(string text) => Convert.FromBase64String(text);
}