using System.Security.Cryptography;

namespace Multifactor.Radius.Adapter.v2.Core.FirstFactor.MsChapV2;

public class Rfc2868
{
    public static byte[] NewTunnelPassword(byte[] password, byte[] salt, byte[] secret, byte[] requestAuthenticator)
    {
        if (password.Length > 249)
            throw new Exception("Invalid password length");

        if (salt.Length != 2)
            throw new Exception("Invalid salt length");

        if ((salt[0] & 0x80) != 0x80) // MSB must be 1
            throw new Exception("Invalid salt");

        if (secret.Length == 0)
            throw new Exception("Invalid secret length");

        if (requestAuthenticator.Length != 16)
            throw new Exception("Invalid request authenticator  length");

        var chunks = (1 + password.Length + 16 - 1) / 16;

        var attr = new byte[2 + chunks * 16];
        Buffer.BlockCopy(salt, 0, attr, 0, salt.Length);
        attr[2] = (byte)password.Length;
        Buffer.BlockCopy(password, 0, attr, 3, password.Length);

        using var md5 = MD5.Create();
        var b = new byte[MD5.HashSizeInBytes];
        for (int chunk = 0; chunk < chunks; chunk++)
        {
            var data = new List<byte>();
            md5.Initialize();
            data.AddRange(secret);

            if (chunk == 0)
            {
                data.AddRange(requestAuthenticator);
                data.AddRange(salt);
            }
            else
            {
                var start = 2 + (chunk - 1) * 16;
                var end = 2 + chunk * 16;
                data.AddRange(attr[start..end]);
            }

            b = md5.ComputeHash(data.ToArray());

            for (var i = 0; i < 16; i++)
            {
                attr[2 + chunk * 16 + i] ^= b[i];
            }
        }

        return attr;
    }

    public static byte[] TunnelPassword(byte[] attrVal, byte[] secret, byte[] requestAuthenticator, out byte[] salt)
    {
        var attrValLen = attrVal.Length;
        if (attrValLen > 252 || attrValLen < 18 || (attrValLen - 2) % 16 != 0)
            throw new ArgumentException("Invalid attribute value length");

        var secretLen = secret.Length;
        if (secretLen == 0)
            throw new AggregateException("Empty secret");

        var reqAuthenticatorLen = requestAuthenticator.Length;
        if (reqAuthenticatorLen != 16)
            throw new AggregateException("invalid requestAuthenticator length");

        if ((attrVal[0] & 0x80) != 0x80) // salt MSB must be 1
            throw new AggregateException("invalid salt");

        var saltList = new List<byte>();
        saltList.AddRange(attrVal[..2]);
        attrVal = attrVal[2..];

        var chunks = attrValLen / 16;
        var plaintext = new byte[chunks * 16];

        using var md5 = MD5.Create();
        var b = new byte[MD5.HashSizeInBytes];

        for (var chunk = 0; chunk < chunks; chunk++)
        {
            var data = new List<byte>();
            md5.Initialize();

            data.AddRange(secret);
            if (chunk == 0)
            {
                data.AddRange(requestAuthenticator);
                data.AddRange(saltList);
            }
            else
            {
                var start = (chunk - 1) * 16;
                var end = chunk * 16;
                data.AddRange(attrVal[start..end]);
            }

            b = md5.ComputeHash(data.ToArray());

            for (var i = 0; i < 16; i++)
            {
                var a = attrVal[chunk * 16 + i];
                plaintext[chunk * 16 + i] = (byte)(a ^ b[i]);
            }
        }

        var passwordLength = plaintext[0];

        if (passwordLength > plaintext.Length - 1)
            throw new Exception("invalid password length");

        salt = saltList.ToArray();
        return plaintext[1..(1 + passwordLength)];
    }

    public static byte[] GenerateSalt()
    {
        var salt = new byte[2];
        RandomNumberGenerator rng = RandomNumberGenerator.Create();
        rng.GetNonZeroBytes(salt);
        salt[0] |= 1 << 7;
        return salt;
    }
}