using System;
using System.Security.Cryptography;
using System.Text;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.ClientLevel;

namespace MultiFactor.Radius.Adapter.Services;

public static class DataProtectionService
{
    public static string Protect(string data)
    {
        if (string.IsNullOrEmpty(data)) throw new ArgumentNullException(data);
        byte[] bytes = StringToBytes(data);
        return ToBase64(bytes);
    }

    public static string Unprotect(string data)
    {
        if (string.IsNullOrEmpty(data)) throw new ArgumentNullException(data);
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