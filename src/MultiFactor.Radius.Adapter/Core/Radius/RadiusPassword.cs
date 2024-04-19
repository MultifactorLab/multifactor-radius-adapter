//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

//MIT License

//Copyright(c) 2017 Verner Fortelius

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace MultiFactor.Radius.Adapter.Core.Radius
{
    public static class RadiusPassword
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
