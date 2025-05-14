using System.Text;

namespace Multifactor.Radius.Adapter.v2.Core.Radius
{
    /// <summary>
    /// Used to encrypt and decrypt user password
    /// </summary>
    public class SharedSecret
    {
        public byte[] Bytes { get; }

        public SharedSecret(string secret)
        {
            if (string.IsNullOrWhiteSpace(secret))
            {
                throw new ArgumentException($"'{nameof(secret)}' cannot be null or whitespace.", nameof(secret));
            }

            Bytes = Encoding.UTF8.GetBytes(secret);
        }

        public SharedSecret(byte[] secret)
        {
            if (secret is null)
            {
                throw new ArgumentNullException(nameof(secret));
            }

            if (secret.Length == 0)
            {
                throw new ArgumentException("Empty secret", nameof(secret));
            }

            Bytes = secret;
        }
    }
}
