using System.Security.Cryptography;
using System.Text;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Radius.Crypto;

public static class RadiusPasswordProtector
{
    public static byte[] Encrypt(SharedSecret secret, RadiusAuthenticator authenticator, byte[] password)
    {
        if (password.Length == 0)
            return password;

        var result = new byte[password.Length];
        var paddedLength = ((password.Length + 15) / 16) * 16;
        var paddedPassword = new byte[paddedLength];
        password.CopyTo(paddedPassword, 0);

        byte[] lastRound = authenticator.Value.ToArray();

        for (int i = 0; i < paddedLength; i += 16)
        {
            using var md5 = MD5.Create();
            md5.TransformBlock(secret.Bytes.ToArray(), 0, secret.Bytes.Length, null, 0);
            md5.TransformFinalBlock(lastRound, 0, 16);
            lastRound = md5.Hash!;

            for (int j = 0; j < 16; j++)
            {
                if (i + j < password.Length)
                {
                    result[i + j] = (byte)(paddedPassword[i + j] ^ lastRound[j]);
                }
            }
        }

        return result;
    }

    public static byte[] Decrypt(SharedSecret secret, RadiusAuthenticator authenticator, byte[] encryptedPassword)
    {
        return Encrypt(secret, authenticator, encryptedPassword); // XOR is symmetric
    }

    public static string EncryptPasswordString(SharedSecret secret, RadiusAuthenticator authenticator, string password)
    {
        var passwordBytes = Encoding.UTF8.GetBytes(password);
        var encryptedBytes = Encrypt(secret, authenticator, passwordBytes);
        return Convert.ToBase64String(encryptedBytes);
    }

    public static string DecryptPasswordString(SharedSecret secret, RadiusAuthenticator authenticator, string encryptedPassword)
    {
        var encryptedBytes = Convert.FromBase64String(encryptedPassword);
        var decryptedBytes = Decrypt(secret, authenticator, encryptedBytes);
        return Encoding.UTF8.GetString(decryptedBytes).TrimEnd('\0');
    }
}