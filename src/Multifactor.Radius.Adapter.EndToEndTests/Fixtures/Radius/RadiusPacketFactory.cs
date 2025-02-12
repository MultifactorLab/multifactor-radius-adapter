using System.Security.Cryptography;
using MultiFactor.Radius.Adapter.Core.Radius;

namespace Multifactor.Radius.Adapter.EndToEndTests.Fixtures.Radius;

internal static class RadiusPacketFactory
{
    public static IRadiusPacket? AccessRequest(SharedSecret? packetSecret = null)
    {
        var header = RadiusPacketHeader.Create(PacketCode.AccessRequest, 0);
        var sharedSecret = packetSecret ?? new SharedSecret(Convert.ToHexString(GenerateSecret()).ToLower());
        var packet = new RadiusPacket(header, new RadiusAuthenticator(), sharedSecret);
        return packet;
    }
    
    public static IRadiusPacket? AccessChallenge(SharedSecret? packetSecret = null)
    {
        var header = RadiusPacketHeader.Create(PacketCode.AccessChallenge, 0);
        var sharedSecret = packetSecret ?? new SharedSecret(Convert.ToHexString(GenerateSecret()).ToLower());
        var packet = new RadiusPacket(header, new RadiusAuthenticator(), sharedSecret);
        return packet;
    }
    
    public static IRadiusPacket? AccessReject(SharedSecret? packetSecret = null)
    {
        var header = RadiusPacketHeader.Create(PacketCode.AccessReject, 0);
        var sharedSecret = packetSecret ?? new SharedSecret(Convert.ToHexString(GenerateSecret()).ToLower());
        var packet = new RadiusPacket(header, new RadiusAuthenticator(), sharedSecret);
        return packet;
    }
    
    public static IRadiusPacket? StatusServer(SharedSecret? packetSecret = null)
    {
        var header = RadiusPacketHeader.Create(PacketCode.StatusServer, 0);
        var sharedSecret = packetSecret ?? new SharedSecret(Convert.ToHexString(GenerateSecret()).ToLower());
        var packet = new RadiusPacket(header, new RadiusAuthenticator(), sharedSecret);
        return packet;
    }

    private static byte[] GenerateSecret()
    {
        using var rng = RandomNumberGenerator.Create();      
        var data = new byte[16];
        // Fill the salt with cryptographically strong byte values.
        rng.GetNonZeroBytes(data);
        return data; 
    }
}