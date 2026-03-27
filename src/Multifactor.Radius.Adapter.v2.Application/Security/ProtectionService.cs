using System.Security.Cryptography;
using System.Text;

namespace Multifactor.Radius.Adapter.v2.Application.Security;

internal static class ProtectionService
{
    public static string Protect(string secret, string data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(data, nameof(data));

        var bytes = Encoding.UTF8.GetBytes(data);
        if (!OperatingSystem.IsWindows()) return Convert.ToBase64String(bytes);
        var additionalEntropy = Encoding.UTF8.GetBytes(secret);
        return Convert.ToBase64String(
            ProtectedData.Protect(bytes, additionalEntropy, DataProtectionScope.CurrentUser));
    }

    public static string Unprotect(string secret, string data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(data, nameof(data));
        
        var bytes = Convert.FromBase64String(data);
        if (!OperatingSystem.IsWindows()) return Encoding.UTF8.GetString(bytes);
        var additionalEntropy = Encoding.UTF8.GetBytes(secret);
        return Encoding.UTF8.GetString(
            ProtectedData.Unprotect(bytes, additionalEntropy, DataProtectionScope.CurrentUser));
    }
}