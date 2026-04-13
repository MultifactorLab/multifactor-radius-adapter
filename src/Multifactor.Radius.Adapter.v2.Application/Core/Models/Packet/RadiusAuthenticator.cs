namespace Multifactor.Radius.Adapter.v2.Application.Core.Models.Packet;

public sealed class RadiusAuthenticator
{
    public byte[] Value { get; }

    public RadiusAuthenticator(byte[] value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Length != 16)
        {
            throw new ArgumentException("Authenticator content length should equal to 16", nameof(value));
        }

        Value = value;
    }
}