using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Core.Radius.Attributes;
using Multifactor.Radius.Adapter.v2.Core.Radius.Metadata;
using Multifactor.Radius.Adapter.v2.Core.Radius.Packet;

namespace Multifactor.Radius.Adapter.v2.Core.Radius;

public class RadiusPacketService : IRadiusPacketService
{
    private readonly ILogger _logger;
    private readonly IRadiusDictionary _radiusDictionary;

    public RadiusPacketService(ILogger<RadiusPacketService> logger, IRadiusDictionary radiusDictionary)
    {
        _logger = logger;
        _radiusDictionary = radiusDictionary;
    }

    public RadiusPacket Parse(
        byte[] packetBytes,
        SharedSecret sharedSecret,
        RadiusAuthenticator requestAuthenticator = null)
    {
        if (packetBytes.Length < RadiusFieldOffsets.LengthFieldPosition + RadiusFieldOffsets.LengthFieldLength)
        {
            throw new InvalidOperationException($"Packet too short: {packetBytes.Length}");
        }

        ushort packetLength = GetPacketLength(packetBytes);
        if (packetBytes.Length != packetLength)
        {
            throw new InvalidOperationException(
                $"Packet length does not match, expected: {packetLength}, actual: {packetBytes.Length}");
        }

        var header = RadiusPacketHeader.Parse(packetBytes);
        var auth = RadiusAuthenticator.Parse(packetBytes);
        var packet = new RadiusPacket(header, auth, requestAuthenticator);

        if (packet.Header.Code == PacketCode.AccountingRequest || packet.Header.Code == PacketCode.DisconnectRequest)
        {
            var requestAuth = CalculateRequestAuthenticator(sharedSecret, packetBytes);
            if (!packet.Authenticator.Value.SequenceEqual(requestAuth))
            {
                throw new InvalidOperationException(
                    $"Invalid request authenticator in packet {packet.Header.Identifier}, check secret?");
            }
        }

        var position = RadiusFieldOffsets.AttributesFieldPosition;
        var messageAuthenticatorPosition = 0;
        while (position < packetBytes.Length)
        {
            var typeCode = packetBytes[position];
            var length = packetBytes[position + 1];

            if (position + length > packetLength)
            {
                throw new ArgumentOutOfRangeException();
            }

            var contentBytes = new byte[length - 2];
            Buffer.BlockCopy(packetBytes, position + 2, contentBytes, 0, length - 2);

            try
            {
                AttributeValue? attribute = null;
                if (typeCode == RadiusAttributeCode.VendorSpecific)
                    attribute = ParseVendorSpecificAttribute(contentBytes, typeCode, packet.Authenticator, sharedSecret);
                else
                    attribute = ParseAttribute(contentBytes, typeCode, packet.Authenticator, sharedSecret);

                if (attribute != null)
                {
                    packet.AddAttributeValue(attribute.Name, attribute!.Value);
                    if (attribute.IsMessageAuthenticator)
                        messageAuthenticatorPosition = position;
                }
            }
            catch (KeyNotFoundException)
            {
                _logger.LogWarning("Attribute {typecode:l} not found in dictionary", typeCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Something went wrong parsing attribute {typecode:l}", typeCode);
            }

            position += length;
        }

        if (messageAuthenticatorPosition != 0)
        {
            var messageAuthenticator = packet.GetAttribute<byte[]>("Message-Authenticator");

            if (!IsMessageAuthenticatorValid(
                    packetBytes,
                    messageAuthenticator,
                    messageAuthenticatorPosition,
                    sharedSecret,
                    requestAuthenticator))
            {
                throw new InvalidOperationException(
                    $"Invalid Message-Authenticator in packet {packet.Header.Identifier}");
            }
        }

        return packet;
    }

    /// <summary>
    /// Get the raw packet bytes
    /// </summary>
    /// <returns></returns>
    public byte[] GetBytes(IRadiusPacket packet, SharedSecret sharedSecret)
    {
        var packetBytes = new List<byte>
        {
            (byte)packet.Header.Code,
            packet.Header.Identifier
        };

        packetBytes.AddRange(new byte[18]); // Placeholder for length and authenticator

        FillAttributes(packetBytes, packet.Authenticator, sharedSecret, packet.Attributes.Values, out int messageAuthenticatorPosition);

        // Note the order of the bytes...
        var packetLengthBytes = BitConverter.GetBytes(packetBytes.Count);
        packetBytes[2] = packetLengthBytes[1];
        packetBytes[3] = packetLengthBytes[0];

        var packetBytesArray = packetBytes.ToArray();
        byte[] authenticator;
        switch (packet.Header.Code)
        {
            case PacketCode.AccountingRequest:
            case PacketCode.DisconnectRequest:
            case PacketCode.CoaRequest:
                if (messageAuthenticatorPosition != 0)
                {
                    FillMessageAuthenticator(packetBytesArray, messageAuthenticatorPosition, sharedSecret);
                }

                authenticator = CalculateRequestAuthenticator(sharedSecret, packetBytesArray);
                Buffer.BlockCopy(authenticator, 0, packetBytesArray, 4, 16);
                break;
            case PacketCode.StatusServer:
                authenticator = packet.RequestAuthenticator != null
                    ? CalculateResponseAuthenticator(sharedSecret, packet.RequestAuthenticator.Value, packetBytesArray)
                    : packet.Authenticator.Value;
                Buffer.BlockCopy(authenticator, 0, packetBytesArray, 4, 16);

                if (messageAuthenticatorPosition != 0)
                {
                    FillMessageAuthenticator(packetBytesArray, messageAuthenticatorPosition, sharedSecret, packet.RequestAuthenticator);
                }
                break;
            default:
                if (packet.RequestAuthenticator == null)
                {
                    Buffer.BlockCopy(packet.Authenticator.Value, 0, packetBytesArray, 4, 16);
                }

                if (messageAuthenticatorPosition != 0)
                {
                    FillMessageAuthenticator(packetBytesArray, messageAuthenticatorPosition, sharedSecret, packet.RequestAuthenticator);
                }

                if (packet.RequestAuthenticator != null)
                {
                    authenticator = CalculateResponseAuthenticator(sharedSecret, packet.RequestAuthenticator.Value, packetBytesArray);
                    Buffer.BlockCopy(authenticator, 0, packetBytesArray, 4, 16);
                }
                break;
        }

        return packetBytesArray;
    }

    public RadiusPacket CreateResponsePacket(IRadiusPacket radiusPacket, PacketCode responsePacketCode)
    {
        if (radiusPacket is null)
            throw new ArgumentNullException(nameof(radiusPacket));
        var header = RadiusPacketHeader.Create(responsePacketCode, radiusPacket.Header.Identifier);
        var packet = new RadiusPacket(header,authenticator: radiusPacket.Authenticator, requestAuthenticator: radiusPacket.Authenticator);
        return packet;
    }

    private void FillMessageAuthenticator(byte[] packetBytesArray, int messageAuthenticatorPosition, SharedSecret sharedSecret, RadiusAuthenticator? requestAuthenticator = null)
    {
        var temp = new byte[16];
        Buffer.BlockCopy(temp, 0, packetBytesArray, messageAuthenticatorPosition + 2, 16);
        var messageAuthenticatorBytes = CalculateMessageAuthenticator(packetBytesArray, sharedSecret, requestAuthenticator);
        Buffer.BlockCopy(messageAuthenticatorBytes, 0, packetBytesArray, messageAuthenticatorPosition + 2, 16);
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
                        headerBytes[0] = RadiusAttributeCode.VendorSpecific; // VSA type

                        var vendorId = BitConverter.GetBytes(vendorAttribute.VendorId);
                        Array.Reverse(vendorId);
                        Buffer.BlockCopy(vendorId, 0, headerBytes, 2, 4);
                        headerBytes[6] = (byte)vendorAttribute.VendorCode;
                        headerBytes[7] = (byte)(2 + contentBytes.Length); // length of the vsa part
                        break;

                    case DictionaryAttribute dictionaryAttribute:
                        headerBytes[0] = attributeType.Code;

                        // Encrypt password if this is a User-Password attribute
                        if (dictionaryAttribute.Code == RadiusAttributeCode.UserPassword)
                        {
                            contentBytes = RadiusPasswordProtector.Encrypt(sharedSecret, authenticator, contentBytes);
                        }
                        else if (dictionaryAttribute.Code == RadiusAttributeCode.MessageAuthenticator) // Remember the position of the message authenticator, because it has to be added after everything else
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

    private AttributeValue? ParseVendorSpecificAttribute(
        byte[] contentBytes,
        byte typeCode,
        RadiusAuthenticator authenticator,
        SharedSecret sharedSecret)
    {
        var vsa = new VendorSpecificAttribute(contentBytes);
        var vendorAttributeDefinition = _radiusDictionary.GetVendorAttribute(vsa.VendorId, vsa.VendorCode);
        if (vendorAttributeDefinition == null)
        {
            _logger.LogDebug("Unknown vsa: {vendorId:l}:{vendorCode:l}", vsa.VendorId, vsa.VendorCode);
            return null;
        }
        else
        {
            try
            {
                var content = ParseContentBytes(
                    vsa.Value,
                    vendorAttributeDefinition.Type,
                    typeCode,
                    authenticator,
                    sharedSecret);

                return new AttributeValue(vendorAttributeDefinition.Name, content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Something went wrong with vsa {vsaName:l}",
                    vendorAttributeDefinition.Name);
                return null;
            }
        }
    }

    private AttributeValue? ParseAttribute(
        byte[] contentBytes,
        byte typeCode,
        RadiusAuthenticator authenticator,
        SharedSecret sharedSecret)
    {
        var attributeDefinition = _radiusDictionary.GetAttribute(typeCode);

        try
        {
            var content = ParseContentBytes(
                contentBytes,
                attributeDefinition.Type,
                typeCode,
                authenticator,
                sharedSecret);

            return new AttributeValue(
                attributeDefinition.Name,
                content,
                attributeDefinition.Code == RadiusAttributeCode.MessageAuthenticator);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Something went wrong with {attributeName:l}", attributeDefinition.Name);
            _logger.LogDebug("Attribute bytes: {contentBytes}", contentBytes.ToHexString());
            return null;
        }
    }

    private static ushort GetPacketLength(byte[] packetBytes)
    {
        var packetLengthbytes = new byte[RadiusFieldOffsets.LengthFieldLength];
        // Length field always third and fourth bytes in packet (rfc2865)
        packetLengthbytes[0] = packetBytes[RadiusFieldOffsets.LengthFieldPosition + 1];
        packetLengthbytes[1] = packetBytes[RadiusFieldOffsets.LengthFieldPosition];
        var packetLength = BitConverter.ToUInt16(packetLengthbytes, 0);
        return packetLength;
    }


    private static byte[] CalculateRequestAuthenticator(SharedSecret sharedSecret, byte[] packetBytes)
    {
        return CalculateResponseAuthenticator(sharedSecret, new byte[16], packetBytes);
    }

    private static byte[] CalculateResponseAuthenticator(SharedSecret sharedSecret, byte[] requestAuthenticator, byte[] packetBytes)
    {
        var responseAuthenticator = packetBytes.Concat(sharedSecret.Bytes).ToArray();
        Buffer.BlockCopy(requestAuthenticator, 0, responseAuthenticator, 4, 16);

        using var md5 = MD5.Create();
        return md5.ComputeHash(responseAuthenticator);
    }

    private static byte[] CalculateMessageAuthenticator(
        byte[] packetBytes,
        SharedSecret sharedSecret,
        RadiusAuthenticator? requestAuthenticator = null)
    {
        var temp = new byte[packetBytes.Length];
        packetBytes.CopyTo(temp, 0);

        requestAuthenticator?.Value.CopyTo(temp, 4);

        using var md5 = new HMACMD5(sharedSecret.Bytes);
        return md5.ComputeHash(temp);
    }

    private static object? ParseContentBytes(
        byte[] contentBytes,
        string type,
        uint code,
        RadiusAuthenticator authenticator,
        SharedSecret sharedSecret)
    {
        switch (type)
        {
            case DictionaryAttribute.TypeTaggedString:
            case DictionaryAttribute.TypeString:
                //couse some NAS (like NPS) send binary within string attributes, check content before unpack to prevent data loss
                if (contentBytes.All(b => b >= 32 && b <= 127)) //only if ascii
                {
                    return Encoding.UTF8.GetString(contentBytes);
                }

                return contentBytes;

            case DictionaryAttribute.TypeOctet:
                // If this is a password attribute it must be decrypted
                if (code == RadiusAttributeCode.UserPassword)
                {
                    return RadiusPasswordProtector.Decrypt(sharedSecret, authenticator, contentBytes);
                }

                return contentBytes;

            case DictionaryAttribute.TypeInteger:
            case DictionaryAttribute.TypeTaggedInteger:
                return BitConverter.ToInt32(contentBytes.Reverse().ToArray(), 0);

            case DictionaryAttribute.TypeIpAddr:
                return new IPAddress(contentBytes);

            default:
                return null;
        }
    }

    private bool IsMessageAuthenticatorValid(
        byte[] packetBytes,
        byte[] messageAuthenticator,
        int messageAuthenticatorPosition,
        SharedSecret sharedSecret,
        RadiusAuthenticator requestAuthenticator)
    {
        var tempPacket = new byte[packetBytes.Length];
        packetBytes.CopyTo(tempPacket, 0);

        // Replace the Message-Authenticator content only.
        // messageAuthenticatorPosition is a position of the Message-Authenticator block.
        // The full-block length is 18: typecode (1), length (1), content (16).
        // So the Message-Authenticator content position is (messageAuthenticatorPosition + 2).
        Buffer.BlockCopy(new byte[16], 0, tempPacket, messageAuthenticatorPosition + 2, 16);

        var calculatedMessageAuthenticator =
            CalculateMessageAuthenticator(tempPacket, sharedSecret, requestAuthenticator);
        return calculatedMessageAuthenticator.SequenceEqual(messageAuthenticator);
    }

    private record AttributeValue(string Name, object? Value, bool IsMessageAuthenticator = false);
}