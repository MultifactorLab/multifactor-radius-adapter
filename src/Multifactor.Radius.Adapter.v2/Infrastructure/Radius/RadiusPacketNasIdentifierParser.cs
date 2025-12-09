using System.Text;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Radius;

public static class RadiusPacketNasIdentifierParser
{
    private const int NasIdentifierAttributeCode = 32;
    private const int PacketHeaderSize = 20;
    private const int AttributeHeaderSize = 2;

    public static bool TryParse(byte[] packetBytes, out string? nasIdentifier)
    {
        nasIdentifier = null;

        if (!ValidatePacketLength(packetBytes))
            return false;

        var position = PacketHeaderSize;
        var packetLength = packetBytes.Length;

        while (position < packetLength)
        {
            var typeCode = packetBytes[position];
            var length = packetBytes[position + 1];

            if (!IsValidAttributeLength(position, length, packetLength))
                return false;

            if (typeCode == NasIdentifierAttributeCode)
            {
                nasIdentifier = ExtractNasIdentifier(packetBytes, position, length);
                return true;
            }

            position += length;
        }

        return false;
    }

    private static bool ValidatePacketLength(byte[] packetBytes)
    {
        if (packetBytes.Length < PacketHeaderSize)
            return false;

        var declaredLength = BitConverter.ToUInt16([packetBytes[3], packetBytes[2]], 0);
        
        if (packetBytes.Length != declaredLength)
            return false;

        return true;
    }

    private static bool IsValidAttributeLength(int position, int length, int packetLength)
    {
        if (length < AttributeHeaderSize)
            return false;

        if (position + length > packetLength)
            return false;

        return true;
    }

    private static string ExtractNasIdentifier(byte[] packetBytes, int position, int length)
    {
        var contentLength = length - AttributeHeaderSize;
        var contentBytes = new byte[contentLength];
        
        Buffer.BlockCopy(packetBytes, position + AttributeHeaderSize, contentBytes, 0, contentLength);
        
        return Encoding.UTF8.GetString(contentBytes);
    }
}