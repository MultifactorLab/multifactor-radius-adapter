using System.Text;

namespace Multifactor.Radius.Adapter.v2.Core.Radius
{
    //Simple radius parser to extract NAS-Identifier attrbute
    public static class RadiusPacketNasIdentifierParser
    {
        private const int NasIdentifierAttributeCode = 32;

        public static bool TryParse(byte[] packetBytes, out string? nasIdentifier)
        {
            nasIdentifier = null;

            var packetLength = BitConverter.ToUInt16(packetBytes.Skip(2).Take(2).Reverse().ToArray(), 0);
            if (packetBytes.Length != packetLength)
            {
                throw new InvalidOperationException($"Packet length does not match, expected: {packetLength}, actual: {packetBytes.Length}");
            }

            var position = 20;
            while (position < packetBytes.Length)
            {
                var typecode = packetBytes[position];
                var length = packetBytes[position + 1];

                if (position + length > packetLength)
                {
                    throw new ArgumentOutOfRangeException("Invalid packet length");
                }

                if (typecode == NasIdentifierAttributeCode)
                {
                    var contentBytes = new byte[length - 2];
                    Buffer.BlockCopy(packetBytes, position + 2, contentBytes, 0, length - 2);

                    nasIdentifier = Encoding.UTF8.GetString(contentBytes);

                    return true;
                }

                position += length;
            }

            return false;
        }

    }
}
