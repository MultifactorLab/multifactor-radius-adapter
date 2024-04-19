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
using System.Security.Cryptography;
using MultiFactor.Radius.Adapter.Core.Radius.Metadata;

namespace MultiFactor.Radius.Adapter.Core.Radius
{
    public class RadiusPacketHeader
    {
        public PacketCode Code { get; }
        public byte Identifier { get; }
        public byte[] Authenticator { get; }

        private RadiusPacketHeader(PacketCode code, byte identifier, byte[] authenticator)
        {
            Code = code;
            Identifier = identifier;
            Authenticator = authenticator;
        }

        public static RadiusPacketHeader Parse(byte[] packet)
        {
            if (packet is null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var code = (PacketCode)packet[RadiusFieldOffsets.CodeFieldPosition];
            var identifier = packet[RadiusFieldOffsets.IdentifierFieldPosition];

            var authenticator = new byte[RadiusFieldOffsets.AuthenticatorFieldLength];
            Buffer.BlockCopy(packet, RadiusFieldOffsets.AuthenticatorFieldPosition, authenticator, 0, RadiusFieldOffsets.AuthenticatorFieldLength);

            return new RadiusPacketHeader(code, identifier, authenticator);
        }

        public static RadiusPacketHeader Create(PacketCode code, byte identifier)
        {
            var auth = new byte[RadiusFieldOffsets.AuthenticatorFieldLength];
            // Generate random authenticator for access request packets
            if (code == PacketCode.AccessRequest || code == PacketCode.StatusServer)
            {
                using var csp = RandomNumberGenerator.Create();
                csp.GetNonZeroBytes(auth);
            }

            return new RadiusPacketHeader(code, identifier, auth);
        }
    }
}
