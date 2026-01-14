using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models.Enums;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Ports;
using Multifactor.Radius.Adapter.v2.Infrastructure.Radius.Crypto;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Radius.Parsers;

public class RadiusPacketParser : IRadiusPacketParser
{
    private readonly IRadiusAttributeParser _attributeParser;
    private readonly IRadiusCryptoProvider  _cryptoProvider;
    private readonly ILogger<RadiusPacketParser> _logger;

    public RadiusPacketParser(
        IRadiusAttributeParser attributeParser,
        IRadiusCryptoProvider  cryptoProvider,
        ILogger<RadiusPacketParser> logger)
    {
        _attributeParser = attributeParser ?? throw new ArgumentNullException(nameof(attributeParser));
        _cryptoProvider = cryptoProvider ?? throw new ArgumentNullException(nameof(cryptoProvider));
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

        var code = (PacketCode)packetBytes[0];
        var identifier = packetBytes[1];
        var authenticatorBytes = new byte[16];
        Buffer.BlockCopy(packetBytes, 4, authenticatorBytes, 0, 16);
        var authenticator = new RadiusAuthenticator(authenticatorBytes);

        var header = new RadiusPacketHeader(code, identifier, authenticator);
        var packet = new RadiusPacket(header, requestAuthenticator);
        
        ParseAttributes(packetBytes, packet, sharedSecret);
        
        return packet;
    }

    private void ValidatePacketLength(byte[] packetBytes)
    {
        if (packetBytes.Length < 20)
        {
            throw new InvalidOperationException($"Packet too short: {packetBytes.Length} bytes. Minimum is 20 bytes.");
        }
    }

    private void ValidatePacketLengthField(byte[] packetBytes)
    {
        var declaredLength = BitConverter.ToUInt16(new[] { packetBytes[3], packetBytes[2] }, 0);
        
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
        int position = 20;
        int messageAuthenticatorPosition = -1;
        byte[] messageAuthenticator = null;

        while (position < packetBytes.Length)
        {
            if (position + 1 >= packetBytes.Length)
            {
                throw new InvalidOperationException("Invalid attribute: incomplete header");
            }

            byte typeCode = packetBytes[position];
            byte length = packetBytes[position + 1];

            if (length < 2)
            {
                throw new InvalidOperationException($"Invalid attribute length: {length}");
            }

            if (position + length > packetBytes.Length)
            {
                throw new InvalidOperationException(
                    $"Attribute exceeds packet boundary. Position: {position}, Length: {length}, Packet Length: {packetBytes.Length}");
            }

            var attributeData = new byte[length];
            Buffer.BlockCopy(packetBytes, position, attributeData, 0, length);

            try
            {
                var parsedAttribute = _attributeParser.Parse(attributeData, packet.Authenticator, sharedSecret);
                
                if (parsedAttribute != null)
                {
                    packet.AddAttributeValue(parsedAttribute.Name, parsedAttribute.Value);
                    
                    if (parsedAttribute.IsMessageAuthenticator)
                    {
                        messageAuthenticatorPosition = position;
                        if (parsedAttribute.Value is byte[] authBytes)
                        {
                            messageAuthenticator = authBytes;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse attribute type {TypeCode} at position {Position}", 
                    typeCode, position);
            }

            position += length;
        }

        if (messageAuthenticatorPosition != -1 && messageAuthenticator != null)
        {
            var isValid = _cryptoProvider.ValidateMessageAuthenticator(
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