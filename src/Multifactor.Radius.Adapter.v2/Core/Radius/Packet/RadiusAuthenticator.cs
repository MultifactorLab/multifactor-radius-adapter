using Multifactor.Radius.Adapter.v2.Core.Radius.Metadata;

namespace Multifactor.Radius.Adapter.v2.Core.Radius.Packet
{
    public class RadiusAuthenticator
    {
        public byte[] Value { get; }

        public RadiusAuthenticator()
        {
            Value = new byte[16];
        }

        public RadiusAuthenticator(byte[] value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value.Length != 16)
            {
                throw new ArgumentException("Authenticator content length should equal to 16", nameof(value));
            }

            Value = value;
        }

        public static RadiusAuthenticator Parse(byte[] packetBytes)
        {
            if (packetBytes is null)
            {
                throw new ArgumentNullException(nameof(packetBytes));
            }

            var authenticator = new byte[RadiusFieldOffsets.AuthenticatorFieldLength];
            Buffer.BlockCopy(packetBytes, RadiusFieldOffsets.AuthenticatorFieldPosition, authenticator, 0, RadiusFieldOffsets.AuthenticatorFieldLength);

            return new RadiusAuthenticator(authenticator);
        }
    }
}
