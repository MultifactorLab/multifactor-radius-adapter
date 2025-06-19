using System.Security.Cryptography;
using System.Text;

namespace Multifactor.Radius.Adapter.v2.Services.DataProtection;

public class WindowsProtectionService : IDataProtectionService
{
    public string Protect(string secret, string data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret, nameof(secret));
        ArgumentException.ThrowIfNullOrWhiteSpace(data, nameof(data));
        
        var additionalEntropy = StringToBytes(secret);
        return ToBase64(ProtectedData.Protect(StringToBytes(data), additionalEntropy, DataProtectionScope.CurrentUser));
    }

    public string Unprotect(string secret, string data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret, nameof(secret));
        ArgumentException.ThrowIfNullOrWhiteSpace(data, nameof(data));
        
        var additionalEntropy = StringToBytes(secret);
        return BytesToString(ProtectedData.Unprotect(FromBase64(data), additionalEntropy, DataProtectionScope.CurrentUser));
    }
    
    private byte[] StringToBytes(string s)
    {
        return Encoding.UTF8.GetBytes(s);
    }

    private string BytesToString(byte[] b)
    {
        return Encoding.UTF8.GetString(b);
    }

    private string ToBase64(byte[] data)
    {
        return Convert.ToBase64String(data);
    }

    private byte[] FromBase64(string text)
    {
        return Convert.FromBase64String(text);
    }
}