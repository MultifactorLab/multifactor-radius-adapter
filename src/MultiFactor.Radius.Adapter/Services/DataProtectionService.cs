using System;
using System.Text;

namespace MultiFactor.Radius.Adapter.Services;

public class DataProtectionService
{
    public string Protect(string data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(data, nameof(data));

        byte[] bytes = StringToBytes(data);
        return ToBase64(bytes);
    }

    public string Unprotect(string data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(data, nameof(data));
        
        byte[] bytes = FromBase64(data);
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