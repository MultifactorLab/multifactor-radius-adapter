using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Multifactor.Radius.Adapter.v2.Core.FirstFactor.LdapAuth.MsChapV2;

namespace Multifactor.Radius.Adapter.v2.Core.FirstFactor.MsChapV2;

public class Rfc2759
{
    private static readonly byte[] _magic1 =
    {
        0x4D, 0x61, 0x67, 0x69, 0x63, 0x20, 0x73, 0x65, 0x72, 0x76,
        0x65, 0x72, 0x20, 0x74, 0x6F, 0x20, 0x63, 0x6C, 0x69, 0x65,
        0x6E, 0x74, 0x20, 0x73, 0x69, 0x67, 0x6E, 0x69, 0x6E, 0x67,
        0x20, 0x63, 0x6F, 0x6E, 0x73, 0x74, 0x61, 0x6E, 0x74,
    };
    
    private static readonly byte[] _magic2 =
    {
        0x50, 0x61, 0x64, 0x20, 0x74, 0x6F, 0x20, 0x6D, 0x61, 0x6B,
        0x65, 0x20, 0x69, 0x74, 0x20, 0x64, 0x6F, 0x20, 0x6D, 0x6F,
        0x72, 0x65, 0x20, 0x74, 0x68, 0x61, 0x6E, 0x20, 0x6F, 0x6E,
        0x65, 0x20, 0x69, 0x74, 0x65, 0x72, 0x61, 0x74, 0x69, 0x6F,
        0x6E
    };
    
    public static string GenerateAuthenticatorResponse(byte[] authenticatorChallenge, byte[] peerChallenge, byte[] ntResponse, string userName, string password)
    {
        var utf16Password = Encoding.Unicode.GetBytes(password);
        var passwordHash = NTPasswordHash(utf16Password);
        var passwordHashHash = NTPasswordHash(passwordHash);
       
        using SHA1 sha = SHA1.Create();
        var data = new List<byte>();
        data.AddRange(passwordHashHash);
        data.AddRange(ntResponse);
        data.AddRange(_magic1);
        
        var hash = sha.ComputeHash(data.ToArray());

        var challenge = ChallengeHash(authenticatorChallenge, peerChallenge, userName);
        using SHA1 sha2 = SHA1.Create();
        
        data = new List<byte>();
        data.AddRange(hash);
        data.AddRange(challenge);
        data.AddRange(_magic2);
        hash = sha2.ComputeHash(data.ToArray());
        return "S=" + Convert.ToHexString(hash).ToUpper();
    }
    
    public static byte[] NTPasswordHash(byte[] password)
    {
        var md4 = new MD4();
        var data = new List<byte>();
        data.AddRange(password);
        var hash = md4.GetByteHashFromBytes(data.ToArray());
        return hash;
    }
    
    public static byte[] GenerateNTResponse(byte[] authenticatorChallenge, byte[] peerChallenge, string userName, string password)
    {
        var challenge = ChallengeHash(authenticatorChallenge, peerChallenge, userName);
        var utf16Password = Encoding.Unicode.GetBytes(password);

        var passwordHash = NTPasswordHash(utf16Password);

        return ChallengeResponse(challenge, passwordHash);
    }
    
    public static byte[] ChallengeHash(byte[] authenticatorChallenge, byte[] peerChallenge, string userName)
    {
        using SHA1 sha = SHA1.Create();
        var data = new List<byte>();
        data.AddRange(peerChallenge);
        data.AddRange(authenticatorChallenge);
        var userNameBytes = Encoding.ASCII.GetBytes(userName);
        data.AddRange(userNameBytes);

        var hash = sha.ComputeHash(data.ToArray());
        return hash[..8];
    }
    
    public static byte[] ChallengeResponse(byte[] challenge, byte[] passwordHash)
    {
        var zPasswordHash  = new byte[21];
        
        Buffer.BlockCopy(passwordHash, 0, zPasswordHash, 0, passwordHash.Length);
        
        var challengeResponse = new List<byte>(24);
        challengeResponse.AddRange(DESCrypt(zPasswordHash[..7], challenge));
        challengeResponse.AddRange(DESCrypt(zPasswordHash[7..14], challenge));
        challengeResponse.AddRange(DESCrypt(zPasswordHash[14..21], challenge));
        return challengeResponse.ToArray();
    }
    
    public static byte[] DESCrypt(byte[] key, byte[] clear)
    {
        var k = key;
        if (k.Length == 7)
            k = ParityPadDESKey(key);

        using var des = DES.Create();
        des.Key = k;
        des.Mode = CipherMode.ECB;
        des.Padding = PaddingMode.None;

        using ICryptoTransform encryptor = des.CreateEncryptor();
        var encrypted = encryptor.TransformFinalBlock(clear, 0, clear.Length);
        return encrypted;
    }
    
    public static byte[] ParityPadDESKey(byte[] inBytes)
    {
        ulong int64 = 0;
        var outBytes = new byte[8];
        var inBytesLength = inBytes.Length;
        
        for (int i = 0; i < inBytesLength; i++)
        {
            var offset = (8 * (inBytesLength - i - 1));
            int64 |= (ulong)inBytes[i] << offset;
        }
        
        var outBytesLength = outBytes.Length;
        
        for (int i = 0; i < outBytesLength; i++)
        {
            var offset = 7 * (outBytesLength - i - 1);
            var byteVal = (byte)((int64 >> offset) & 0xFF);
            outBytes[i] = (byte)(byteVal << 1);

            if (BitOperations.PopCount(outBytes[i]) % 2 == 0) {
                outBytes[i] |= 1;
            }
        }

        return outBytes;
    }
}