using System.Text;

namespace Multifactor.Radius.Adapter.v2.Features.PacketHandle;

internal static class RadiusNasIdentifierExtractor
{
    private const int NasIdentifierAttributeCode = 32;
    private const int MinimumPacketLength = 20;

    public static bool TryExtract(byte[]? packetBytes, out string nasIdentifier)
    {
        nasIdentifier = string.Empty;

        if (packetBytes == null || packetBytes.Length < MinimumPacketLength)
            return false;

        try
        {
            // Read packet length from bytes 2-3 (network byte order)
            ushort packetLength = BitConverter.ToUInt16([packetBytes[3], packetBytes[2]], 0);
            
            if (packetBytes.Length != packetLength)
                return false;

            int position = 20; // Start of attributes
            
            while (position < packetBytes.Length)
            {
                if (position + 1 >= packetBytes.Length)
                    break;

                byte typeCode = packetBytes[position];
                byte length = packetBytes[position + 1];

                if (length < 2 || position + length > packetBytes.Length)
                    break;

                if (typeCode == NasIdentifierAttributeCode)
                {
                    byte[] contentBytes = new byte[length - 2];
                    Buffer.BlockCopy(packetBytes, position + 2, contentBytes, 0, contentBytes.Length);
                    
                    nasIdentifier = Encoding.UTF8.GetString(contentBytes).TrimEnd('\0');
                    return !string.IsNullOrEmpty(nasIdentifier);
                }

                position += length;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}