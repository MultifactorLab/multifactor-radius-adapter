using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.Adapters.Radius;

public interface IRadiusPacketParser
{
    RadiusPacket Parse(byte[] packetBytes, SharedSecret sharedSecret);
    RadiusPacket Parse(byte[] packetBytes, SharedSecret sharedSecret, RadiusAuthenticator requestAuthenticator);
}

internal sealed class RadiusPacketParser : IRadiusPacketParser
{
    private readonly IRadiusAttributeParser _attributeParser;
    private readonly ILogger<RadiusPacketParser> _logger;

    private const int LengthFieldPosition = 2;
    private const int LengthFieldLength = 2;
    private const int AttributesFieldPosition = 20;
    
    public RadiusPacketParser(
        IRadiusAttributeParser attributeParser,
        ILogger<RadiusPacketParser> logger)
    {
        _attributeParser = attributeParser ?? throw new ArgumentNullException(nameof(attributeParser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public RadiusPacket Parse(byte[] packetBytes, SharedSecret sharedSecret)
    {
        return ParseInternal(packetBytes, sharedSecret, null);
    }

    public RadiusPacket Parse(byte[] packetBytes, SharedSecret sharedSecret, RadiusAuthenticator requestAuthenticator)
    {
        return ParseInternal(packetBytes, sharedSecret, requestAuthenticator);
    }

    private RadiusPacket ParseInternal(
        byte[] packetBytes,
        SharedSecret sharedSecret,
        RadiusAuthenticator? requestAuthenticator)
    {
        ValidatePacketLength(packetBytes);
        ValidatePacketLengthField(packetBytes);

        var header = RadiusPacketHeader.Parse(packetBytes);
        var packet = new RadiusPacket(header, requestAuthenticator);
        
        if (packet.Code == PacketCode.AccountingRequest || packet.Code == PacketCode.DisconnectRequest)
        {
            var requestAuth = RadiusCryptoProvider.CalculateRequestAuthenticator(sharedSecret, packetBytes);
            if (!packet.Authenticator.Value.SequenceEqual(requestAuth))
            {
                throw new InvalidOperationException(
                    $"Invalid request authenticator in packet {packet.Identifier}, check secret?");
            }
        }
        
        ParseAttributes(packetBytes, packet, sharedSecret);
        
        return packet;
    }
    

    private static ushort GetPacketLength(byte[] packetBytes)
    {
        var packetLengthBytes = new byte[LengthFieldLength];
        // Length field always third and fourth bytes in packet (rfc2865)
        packetLengthBytes[0] = packetBytes[LengthFieldPosition + 1];
        packetLengthBytes[1] = packetBytes[LengthFieldPosition];
        var packetLength = BitConverter.ToUInt16(packetLengthBytes, 0);
        return packetLength;
    }
    
    private static void ValidatePacketLength(byte[] packetBytes)
    {
        if (packetBytes.Length < 20)
        {
            throw new InvalidOperationException($"Packet too short: {packetBytes.Length} bytes. Minimum is 20 bytes.");
        }
    }

    private static void ValidatePacketLengthField(byte[] packetBytes)
    {
        var declaredLength = BitConverter.ToUInt16([packetBytes[3], packetBytes[2]], 0);
        
        if (declaredLength != packetBytes.Length)
        {
            throw new InvalidOperationException(
                $"Packet length mismatch. Declared: {declaredLength}, Actual: {packetBytes.Length}");
        }
        
        if (declaredLength > 4096)
        {
            throw new InvalidOperationException(
                $"Packet too large: {declaredLength} bytes. Maximum is 4096 bytes.");
        }
    }

    private void ParseAttributes(
        byte[] packetBytes,
        RadiusPacket packet,
        SharedSecret sharedSecret)
    {
        int position = AttributesFieldPosition;
        int messageAuthenticatorPosition = 0;

        ushort packetLength = GetPacketLength(packetBytes);
        
        while (position < packetBytes.Length)
        {
            var typeCode = packetBytes[position];
            var length = packetBytes[position + 1];

            if (position + length > packetLength)
            {
                throw new ArgumentOutOfRangeException();
            }

            var attributeData = new byte[length - 2];
            Buffer.BlockCopy(packetBytes, position + 2, attributeData, 0, length - 2);

            try
            {
                
                var parsedAttribute = _attributeParser.Parse(attributeData, typeCode, packet.Authenticator, sharedSecret);
                
                if (parsedAttribute != null)
                {
                    packet.AddAttributeValue(parsedAttribute.Name, parsedAttribute.Value);
                    if (parsedAttribute.IsMessageAuthenticator)
                    {
                        messageAuthenticatorPosition = position;
                    }
                }
            }
            catch (KeyNotFoundException)
            {
                _logger.LogWarning("Attribute {typecode:l} not found in dictionary", typeCode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse attribute type {TypeCode} at position {Position}", 
                    typeCode, position);
            }

            position += length;
        }

        if (messageAuthenticatorPosition != 0)
        {
            var messageAuthenticator = packet.GetAttribute<byte[]>("Message-Authenticator");
            var isValid = RadiusCryptoProvider.ValidateMessageAuthenticator(
                packetBytes,
                messageAuthenticator,
                messageAuthenticatorPosition,
                sharedSecret,
                packet.RequestAuthenticator);

            if (!isValid)
            {
                throw new InvalidOperationException("Invalid Message-Authenticator");
            }
        }
    }
}