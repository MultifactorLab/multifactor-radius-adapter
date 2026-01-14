namespace Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models
{
    public class RadiusAuthenticator
    {
        private const int AuthenticatorFieldLength = 16;
        private const int AuthenticatorFieldPosition = 4;
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

            var authenticator = new byte[AuthenticatorFieldLength];
            Buffer.BlockCopy(packetBytes, AuthenticatorFieldPosition, authenticator, 0, AuthenticatorFieldLength);

            return new RadiusAuthenticator(authenticator);
        }
    }
}