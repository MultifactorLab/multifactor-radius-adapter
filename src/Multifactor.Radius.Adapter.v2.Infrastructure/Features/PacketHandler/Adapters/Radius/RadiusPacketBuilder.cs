using System.Net;
using System.Text;
using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Packet;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Models.Dictionary;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Models.Dictionary.Attributes;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.Adapters.Radius;

internal interface IRadiusPacketBuilder
{
    byte[] Build(RadiusPacket packet, SharedSecret sharedSecret);
    RadiusPacket CreateResponse(RadiusPacket request, PacketCode responseCode);
}

internal sealed class RadiusPacketBuilder : IRadiusPacketBuilder
{
    /// <summary>
    /// User-Password
    /// </summary>
    private const int UserPassword = 2;

    /// <summary>
    /// Vendor-Specific
    /// </summary>
    private const int VendorSpecific = 26;

    /// <summary>
    /// Message-Authenticator
    /// </summary>
    private const int MessageAuthenticator = 80;
    
    private readonly IRadiusDictionary _radiusDictionary;

    public RadiusPacketBuilder(
        IRadiusDictionary radiusDictionary)
    {
        _radiusDictionary = radiusDictionary ?? throw new ArgumentNullException(nameof(radiusDictionary));
    }

    public byte[] Build(RadiusPacket packet, SharedSecret sharedSecret)
    {
        ArgumentNullException.ThrowIfNull(packet);
        ArgumentNullException.ThrowIfNull(sharedSecret);


        var packetBytes = new List<byte>
        {
            // Header: Code (1), Identifier (1), Length (2), Authenticator (16)
            (byte)packet.Code,
            packet.Identifier
        };

        packetBytes.AddRange(new byte[18]); // Placeholder for length and authenticator
        
        // Serialize attributes
        FillAttributes(packetBytes, packet.Authenticator, sharedSecret, packet.Attributes.Values, out int messageAuthenticatorPosition);
        
        // Set packet length
        ushort packetLength = (ushort)packetBytes.Count;
        var lengthBytes = BitConverter.GetBytes(packetLength);
        packetBytes[2] = lengthBytes[1];
        packetBytes[3] = lengthBytes[0];
        
        var packetBytesArray = packetBytes.ToArray();
        
        // Calculate authenticator based on packet type
        byte[] authenticator;
        switch (packet.Code)
        {
            case PacketCode.AccountingRequest:
            case PacketCode.DisconnectRequest:
            case PacketCode.CoaRequest:
                if (messageAuthenticatorPosition != 0)
                {
                    FillMessageAuthenticator(packetBytesArray, messageAuthenticatorPosition, sharedSecret);
                }
                authenticator = RadiusCryptoProvider.CalculateRequestAuthenticator(sharedSecret, packetBytesArray);
                Buffer.BlockCopy(authenticator, 0, packetBytesArray, 4, 16);
                break;
                
            case PacketCode.StatusServer:
                authenticator = packet.RequestAuthenticator != null
                    ? RadiusCryptoProvider.CalculateResponseAuthenticator(
                        sharedSecret, 
                        packet.RequestAuthenticator.Value.ToArray(), 
                        packetBytesArray)
                    : packet.Authenticator.Value.ToArray();
                    
                if (messageAuthenticatorPosition != 0)
                {
                    FillMessageAuthenticator(
                        packetBytesArray, 
                        messageAuthenticatorPosition, 
                        sharedSecret, 
                        packet.RequestAuthenticator);
                }                   
                Buffer.BlockCopy(authenticator, 0, packetBytesArray, 4, 16);

                break;
                
            default:
                if (packet.RequestAuthenticator == null)
                {                    
                    Buffer.BlockCopy(packet.Authenticator.Value, 0, packetBytesArray, 4, 16);
                }
                
                if (messageAuthenticatorPosition != 0)
                {
                    FillMessageAuthenticator(
                        packetBytesArray,
                        messageAuthenticatorPosition,
                        sharedSecret,
                        packet.RequestAuthenticator);
                }
        
                if (packet.RequestAuthenticator != null)
                {
                    authenticator = RadiusCryptoProvider.CalculateResponseAuthenticator(
                        sharedSecret,
                        packet.RequestAuthenticator.Value.ToArray(),
                        packetBytesArray);
                    Buffer.BlockCopy(authenticator, 0, packetBytesArray, 4, 16);
                }
                break;
        }
        
        
        return packetBytesArray;
    }

    public RadiusPacket CreateResponse(RadiusPacket request, PacketCode responseCode)
    {
        ArgumentNullException.ThrowIfNull(request);

        var header = RadiusPacketHeader.Create(responseCode, request.Identifier);
        var response = new RadiusPacket(header, requestAuthenticator: request.Authenticator);

        return response;
    }
    
    private void FillAttributes(List<byte> packetBytes, RadiusAuthenticator authenticator, SharedSecret sharedSecret, IEnumerable<RadiusAttribute> attributes, out int messageAuthenticatorPosition)
    {
        messageAuthenticatorPosition = 0;
        foreach (var attribute in attributes)
        {
            var attributeValues = attribute.Values;
            foreach (var value in attributeValues)
            {
                var contentBytes = GetAttributeValueBytes(value);
                var headerBytes = new byte[2];
    
                var attributeType = _radiusDictionary.GetAttribute(attribute.Name);
                switch (attributeType)
                {
                    case DictionaryVendorAttribute vendorAttribute:
                        headerBytes = new byte[8];
                        headerBytes[0] = VendorSpecific; // VSA type
    
                        var vendorId = BitConverter.GetBytes(vendorAttribute.VendorId);
                        Array.Reverse(vendorId);
                        Buffer.BlockCopy(vendorId, 0, headerBytes, 2, 4);
                        headerBytes[6] = (byte)vendorAttribute.VendorCode;
                        headerBytes[7] = (byte)(2 + contentBytes.Length); // length of the vsa part
                        break;
    
                    case DictionaryAttribute dictionaryAttribute:
                        headerBytes[0] = attributeType.Code;
    
                        // Encrypt password if this is a User-Password attribute
                        if (dictionaryAttribute.Code == UserPassword)
                        {
                            contentBytes = RadiusPasswordProtector.Encrypt(sharedSecret, authenticator, contentBytes);
                        }
                        else if (dictionaryAttribute.Code == MessageAuthenticator) // Remember the position of the message authenticator, because it has to be added after everything else
                        {
                            messageAuthenticatorPosition = packetBytes.Count;
                        }
    
                        break;
                    default:
                        throw new InvalidOperationException(
                            "Unknown attribute {attribute.Key}, check spelling or dictionary");
                }
    
                headerBytes[1] = (byte)(headerBytes.Length + contentBytes.Length);
                packetBytes.AddRange(headerBytes);
                packetBytes.AddRange(contentBytes);
            }
        }
    }
    
    /// <summary>
    /// Gets the byte representation of an attribute object
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    private static byte[] GetAttributeValueBytes(object value)
    {
        switch (value)
        {
            case string val:
                return Encoding.UTF8.GetBytes(val);
            case uint val:
                var contentBytes = BitConverter.GetBytes(val);
                Array.Reverse(contentBytes);
                return contentBytes;
            case int val:
                contentBytes = BitConverter.GetBytes(val);
                Array.Reverse(contentBytes);
                return contentBytes;
            case byte[] val:
                return val;
            case IPAddress val:
                return val.GetAddressBytes();
            default:
                throw new NotImplementedException();
        }
    }
    
    private static void FillMessageAuthenticator(
        byte[] packetBytes,
        int position,
        SharedSecret sharedSecret,
        RadiusAuthenticator? requestAuthenticator = null)
    {
    
        var temp = new byte[16];
        Buffer.BlockCopy(temp, 0, packetBytes, position + 2, 16);
        var messageAuthenticator = RadiusCryptoProvider.CalculateMessageAuthenticator(
            sharedSecret,
            packetBytes,
            requestAuthenticator);
        Buffer.BlockCopy(messageAuthenticator, 0, packetBytes, position + 2, 16);
    }
}