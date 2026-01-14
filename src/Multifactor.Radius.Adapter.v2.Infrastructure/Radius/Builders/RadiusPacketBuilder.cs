using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models.Enums;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Ports;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Dictionary;
using Multifactor.Radius.Adapter.v2.Infrastructure.Radius.Crypto;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Radius.Builders;

public class RadiusPacketBuilder : IRadiusPacketBuilder
{
    private readonly IRadiusDictionary _radiusDictionary;
    private readonly IRadiusCryptoProvider _cryptoProvider;
    private readonly ILogger<RadiusPacketBuilder> _logger;
    private readonly IRadiusAttributeSerializer _attributeSerializer;

    public RadiusPacketBuilder(
        IRadiusDictionary radiusDictionary,
        IRadiusCryptoProvider cryptoProvider,
        IRadiusAttributeSerializer attributeSerializer,
        ILogger<RadiusPacketBuilder> logger)
    {
        _radiusDictionary = radiusDictionary ?? throw new ArgumentNullException(nameof(radiusDictionary));
        _cryptoProvider = cryptoProvider ?? throw new ArgumentNullException(nameof(cryptoProvider));
        _attributeSerializer = attributeSerializer ?? throw new ArgumentNullException(nameof(attributeSerializer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public byte[] Build(RadiusPacket packet, SharedSecret sharedSecret)
    {
        if (packet == null) throw new ArgumentNullException(nameof(packet));
        if (sharedSecret == null) throw new ArgumentNullException(nameof(sharedSecret));

        var packetBytes = new List<byte>();
        
        // Header: Code (1), Identifier (1), Length (2), Authenticator (16)
        packetBytes.Add((byte)packet.Code);
        packetBytes.Add(packet.Identifier);
        packetBytes.AddRange(new byte[2]); // Placeholder for length
        packetBytes.AddRange(new byte[16]); // Placeholder for authenticator

        int messageAuthenticatorPosition = -1;
        
        // Serialize attributes
        foreach (var attribute in packet.Attributes.Values)
        {
            foreach (var value in attribute.Values)
            {
                var attributeBytes = _attributeSerializer.Serialize(
                    attribute.Name,
                    value,
                    packet.Authenticator,
                    sharedSecret,
                    packet.RequestAuthenticator);
                
                if (attributeBytes != null)
                {
                    // Check if this is Message-Authenticator
                    var attributeDefinition = _radiusDictionary.GetAttribute(attribute.Name);
                    if (attributeDefinition?.Code == 80) // Message-Authenticator
                    {
                        messageAuthenticatorPosition = packetBytes.Count;
                    }
                    
                    packetBytes.AddRange(attributeBytes);
                }
            }
        }

        // Set packet length
        ushort packetLength = (ushort)packetBytes.Count;
        var lengthBytes = BitConverter.GetBytes(packetLength);
        Array.Reverse(lengthBytes); // Network byte order
        packetBytes[2] = lengthBytes[0];
        packetBytes[3] = lengthBytes[1];

        var packetBytesArray = packetBytes.ToArray();
        
        // Calculate authenticator based on packet type
        byte[] authenticator;
        switch (packet.Code)
        {
            case PacketCode.AccountingRequest:
            case PacketCode.DisconnectRequest:
            case PacketCode.CoaRequest:
                if (messageAuthenticatorPosition != -1)
                {
                    FillMessageAuthenticator(packetBytesArray, messageAuthenticatorPosition, sharedSecret);
                }
                authenticator = _cryptoProvider.CalculateRequestAuthenticator(sharedSecret, packetBytesArray);
                break;
                
            case PacketCode.StatusServer:
                authenticator = packet.RequestAuthenticator != null
                    ? _cryptoProvider.CalculateResponseAuthenticator(
                        sharedSecret, 
                        packet.RequestAuthenticator.Value.ToArray(), 
                        packetBytesArray)
                    : packet.Authenticator.Value.ToArray();
                    
                if (messageAuthenticatorPosition != -1)
                {
                    FillMessageAuthenticator(
                        packetBytesArray, 
                        messageAuthenticatorPosition, 
                        sharedSecret, 
                        packet.RequestAuthenticator);
                }
                break;
                
            default:
                if (packet.RequestAuthenticator != null)
                {
                    authenticator = _cryptoProvider.CalculateResponseAuthenticator(
                        sharedSecret,
                        packet.RequestAuthenticator.Value.ToArray(),
                        packetBytesArray);
                }
                else
                {
                    authenticator = packet.Authenticator.Value.ToArray();
                }
                
                if (messageAuthenticatorPosition != -1)
                {
                    FillMessageAuthenticator(
                        packetBytesArray,
                        messageAuthenticatorPosition,
                        sharedSecret,
                        packet.RequestAuthenticator);
                }
                break;
        }

        // Copy authenticator to packet
        Buffer.BlockCopy(authenticator, 0, packetBytesArray, 4, 16);
        
        return packetBytesArray;
    }

    public RadiusPacket CreateResponse(RadiusPacket request, PacketCode responseCode)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        
        var header = RadiusPacketHeader.Create(responseCode, request.Identifier);
        var response = new RadiusPacket(header, requestAuthenticator: request.Authenticator);

        return response;
    }

    private void FillMessageAuthenticator(
        byte[] packetBytes,
        int position,
        SharedSecret sharedSecret,
        RadiusAuthenticator? requestAuthenticator = null)
    {
        // Zero out the Message-Authenticator field
        for (int i = 0; i < 16; i++)
        {
            packetBytes[position + 2 + i] = 0;
        }

        // Calculate and insert Message-Authenticator
        var messageAuthenticator = _cryptoProvider.CalculateMessageAuthenticator(
            sharedSecret,
            packetBytes,
            requestAuthenticator);
        
        Buffer.BlockCopy(messageAuthenticator, 0, packetBytes, position + 2, 16);
    }
}