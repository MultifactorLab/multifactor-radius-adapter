using System.Security.Cryptography;
using System.Text;
using Multifactor.Radius.Adapter.v2.Core.Radius.Packet;

namespace Multifactor.Radius.Adapter.v2.Core.Radius
{
    public static class RadiusPasswordProtector
    {
        /// <summary>
        /// Encrypt/decrypt using XOR
        /// </summary>
        /// <param name="input"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        private static byte[] EncryptDecrypt(byte[] input, byte[] key)
        {
            var output = new byte[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                output[i] = (byte)(input[i] ^ key[i]);
            }
            return output;
        }


        /// <summary>
        /// Create a radius shared secret key
        /// </summary>
        /// <param name="sharedSecret"></param>
        /// <param name="Stuff"></param>
        /// <returns></returns>
        private static byte[] CreateKey(SharedSecret sharedSecret, RadiusAuthenticator authenticator)
        {
            var key = new byte[16 + sharedSecret.Bytes.Length];
            Buffer.BlockCopy(sharedSecret.Bytes, 0, key, 0, sharedSecret.Bytes.Length);
            Buffer.BlockCopy(authenticator.Value, 0, key, sharedSecret.Bytes.Length, authenticator.Value.Length);

            using var md5 = MD5.Create();
            return md5.ComputeHash(key);
        }


        /// <summary>
        /// Decrypt user password
        /// </summary>
        /// <param name="sharedSecret"></param>
        /// <param name="authenticator"></param>
        /// <param name="passwordBytes"></param>
        /// <returns></returns>
        public static string Decrypt(SharedSecret sharedSecret, RadiusAuthenticator authenticator, byte[] passwordBytes)
        {
            var key = CreateKey(sharedSecret, authenticator);
            var bytes = new List<byte>();

            for (var n = 1; n <= passwordBytes.Length / 16; n++)
            {
                var temp = new byte[16];
                Buffer.BlockCopy(passwordBytes, (n - 1) * 16, temp, 0, 16);

                var block = EncryptDecrypt(temp, key);
                bytes.AddRange(block);

                key = CreateKey(sharedSecret, new RadiusAuthenticator(temp));
            }

            var ret = Encoding.UTF8.GetString(bytes.ToArray());
            return ret.Replace("\0", "");
        }


        /// <summary>
        /// Encrypt a password
        /// </summary>
        /// <param name="sharedSecret"></param>
        /// <param name="authenticator"></param>
        /// <param name="passwordBytes"></param>
        /// <returns></returns>
        public static byte[] Encrypt(SharedSecret sharedSecret, RadiusAuthenticator authenticator, byte[] passwordBytes)
        {
            Array.Resize(ref passwordBytes, passwordBytes.Length + (16 - passwordBytes.Length % 16));

            var key = CreateKey(sharedSecret, authenticator);
            var bytes = new List<byte>();
            for (var n = 1; n <= passwordBytes.Length / 16; n++)
            {
                var temp = new byte[16];
                Buffer.BlockCopy(passwordBytes, (n - 1) * 16, temp, 0, 16);
                var xor = EncryptDecrypt(temp, key);
                bytes.AddRange(xor);
                key = CreateKey(sharedSecret, new RadiusAuthenticator(xor));
            }

            return bytes.ToArray();
        }
    }
}
