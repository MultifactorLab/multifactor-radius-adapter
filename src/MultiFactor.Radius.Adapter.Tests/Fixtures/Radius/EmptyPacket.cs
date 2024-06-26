﻿using MultiFactor.Radius.Adapter.Core.Radius;
using System.Security.Cryptography;

namespace MultiFactor.Radius.Adapter.Tests.Fixtures.Radius;

internal static class RadiusPacketFactory
{
    public static IRadiusPacket AccessRequest()
    {
        var header = RadiusPacketHeader.Create(PacketCode.AccessRequest, 0);
        var secret = Convert.ToHexString(GenerateSecret()).ToLower();
        var sharedSecret = new SharedSecret(secret);
        var packet = new RadiusPacket(header, new RadiusAuthenticator(), sharedSecret);
        return packet;
    }
    
    public static IRadiusPacket AccessChallenge()
    {
        var header = RadiusPacketHeader.Create(PacketCode.AccessChallenge, 0);
        var secret = Convert.ToHexString(GenerateSecret()).ToLower();
        var sharedSecret = new SharedSecret(secret);
        var packet = new RadiusPacket(header, new RadiusAuthenticator(), sharedSecret);
        return packet;
    }
    
    public static IRadiusPacket AccessReject()
    {
        var header = RadiusPacketHeader.Create(PacketCode.AccessReject, 0);
        var secret = Convert.ToHexString(GenerateSecret()).ToLower();
        var sharedSecret = new SharedSecret(secret);
        var packet = new RadiusPacket(header, new RadiusAuthenticator(), sharedSecret);
        return packet;
    }
    
    public static IRadiusPacket StatusServer()
    {
        var header = RadiusPacketHeader.Create(PacketCode.StatusServer, 0);
        var secret = Convert.ToHexString(GenerateSecret()).ToLower();
        var sharedSecret = new SharedSecret(secret);
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