using System.Security.Cryptography;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models.Enums;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models
{
    /// <summary>
    /// Radius packet header model 
    /// See https://datatracker.ietf.org/doc/html/rfc2865#section-3
    /// </summary>
    public class RadiusPacketHeader
    {
        public const int CodeFieldPosition = 0;
        public const int IdentifierFieldPosition = 1;
        public const int AuthenticatorFieldPosition = 4;
        public const int AuthenticatorFieldLength = 16;
        public PacketCode Code { get; }
        public byte Identifier { get; }
        public RadiusAuthenticator Authenticator { get; }
        
        public RadiusPacketHeader(){}
        public RadiusPacketHeader(PacketCode code, byte identifier, byte[] authenticator)
        {
            ArgumentNullException.ThrowIfNull(authenticator, nameof(authenticator));
            
            Code = code;
            Identifier = identifier;
            Authenticator = new RadiusAuthenticator(authenticator);
        }
        
        public RadiusPacketHeader(PacketCode code, byte identifier, RadiusAuthenticator authenticator)
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

            var code = (PacketCode)packet[CodeFieldPosition];
            var identifier = packet[IdentifierFieldPosition];

            var authenticator = new byte[AuthenticatorFieldLength];
            Buffer.BlockCopy(packet, AuthenticatorFieldPosition, authenticator, 0, AuthenticatorFieldLength);

            return new RadiusPacketHeader(code, identifier, authenticator);
        }

        public static RadiusPacketHeader Create(PacketCode code, byte identifier)
        {
            var auth = new byte[AuthenticatorFieldLength];
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
