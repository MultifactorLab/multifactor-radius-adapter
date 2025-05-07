using System.Security.Cryptography;
using Multifactor.Radius.Adapter.v2.Core.Radius.Metadata;

namespace Multifactor.Radius.Adapter.v2.Core.Radius.Packet
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
