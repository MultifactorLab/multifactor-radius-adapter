//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

//MIT License

//Copyright(c) 2017 Verner Fortelius

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Core.Radius.Attributes;
using MultiFactor.Radius.Adapter.Core.Radius.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace MultiFactor.Radius.Adapter.Core.Radius
{
    public class RadiusPacketParser : IRadiusPacketParser
    {
        private readonly ILogger _logger;
        private readonly IRadiusDictionary _radiusDictionary;

        /// <summary>
        /// RadiusPacketParser
        /// </summary>
        /// <param name="logger"></param>
        public RadiusPacketParser(ILogger<RadiusPacketParser> logger, IRadiusDictionary radiusDictionary)
        {
            _logger = logger;
            _radiusDictionary = radiusDictionary;
        }

        /// <summary>
        /// Parses packet bytes and returns an IRadiusPacket
        /// </summary>
        /// <param name="packetBytes"></param>
        /// <param name="dictionary"></param>
        /// <param name="sharedSecret"></param>
        public IRadiusPacket Parse(byte[] packetBytes, SharedSecret sharedSecret, byte[] requestAuthenticator = null,
            Action<RadiusPacketOptions> configure = null)
        {
            if (packetBytes.Length < RadiusFieldOffsets.LengthFieldPosition + RadiusFieldOffsets.LengthFieldLength)
            {
                throw new InvalidOperationException($"Packet too short: {packetBytes.Length}");
            }

            ushort packetLength = GetPacketLength(packetBytes);
            if (packetBytes.Length != packetLength)
            {
                throw new InvalidOperationException($"Packet length does not match, expected: {packetLength}, actual: {packetBytes.Length}");
            }

            var header = RadiusPacketHeader.Parse(packetBytes);
            var auth = RadiusAuthenticator.Parse(packetBytes);
            var packet = new RadiusPacket(header, auth, sharedSecret);

            packet.Configure(configure);

            if (packet.Header.Code == PacketCode.AccountingRequest || packet.Header.Code == PacketCode.DisconnectRequest)
            {
                var requestAuth = CalculateRequestAuthenticator(packet.SharedSecret, packetBytes);
                if (!ArraysAreEqual(packet.Authenticator.Value, requestAuth))
                {
                    throw new InvalidOperationException($"Invalid request authenticator in packet {packet.Header.Identifier}, check secret?");
                }
            }

            // The rest are attribute value pairs
            var position = RadiusFieldOffsets.AttributesFieldPosition;
            var messageAuthenticatorPosition = 0;
            while (position < packetBytes.Length)
            {
                var typecode = packetBytes[position];
                var length = packetBytes[position + 1];

                if (position + length > packetLength)
                {
                    throw new ArgumentOutOfRangeException("Go home roamserver, youre drunk");
                }
                var contentBytes = new byte[length - 2];
                Buffer.BlockCopy(packetBytes, position + 2, contentBytes, 0, length - 2);

                try
                {
                    if (typecode == RadiusAttributeCode.VendorSpecific)
                    {
                        var vsa = new VendorSpecificAttribute(contentBytes);
                        var vendorAttributeDefinition = _radiusDictionary.GetVendorAttribute(vsa.VendorId, vsa.VendorCode);
                        if (vendorAttributeDefinition == null)
                        {
                            _logger.LogDebug("Unknown vsa: {vendorId:l}:{vendorCode:l}", vsa.VendorId, vsa.VendorCode);
                        }
                        else
                        {
                            try
                            {
                                var content = ParseContentBytes(vsa.Value, vendorAttributeDefinition.Type, typecode, packet.Authenticator, packet.SharedSecret);
                                packet.AddAttributeObject(vendorAttributeDefinition.Name, content);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Something went wrong with vsa {vsaName:l}", vendorAttributeDefinition.Name);
                            }
                        }
                    }
                    else
                    {
                        var attributeDefinition = _radiusDictionary.GetAttribute(typecode);
                        if (attributeDefinition.Code == RadiusAttributeCode.MessageAuthenticator)
                        {
                            messageAuthenticatorPosition = position;
                        }
                        try
                        {
                            var content = ParseContentBytes(contentBytes, attributeDefinition.Type, typecode, packet.Authenticator, packet.SharedSecret);
                            packet.AddAttributeObject(attributeDefinition.Name, content);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Something went wrong with {attributeName:l}", attributeDefinition.Name);
                            _logger.LogDebug("Attribute bytes: {contentBytes}", contentBytes.ToHexString());
                        }
                    }
                }
                catch (KeyNotFoundException)
                {
                    _logger.LogWarning("Attribute {typecode:l} not found in dictionary", typecode);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Something went wrong parsing attribute {typecode:l}", typecode);
                }

                position += length;
            }

            if (messageAuthenticatorPosition != 0)
            {
                var messageAuthenticator = packet.GetAttribute<byte[]>("Message-Authenticator");

                var tempPacket = new byte[packetBytes.Length];
                packetBytes.CopyTo(tempPacket, 0);

                // Replace the Message-Authenticator content only.
                // messageAuthenticatorPosition is a position of the Message-Authenticator block.
                // The full-block length is 18: typecode (1), length (1), content (16).
                // So the Message-Authenticator content position is (messageAuthenticatorPosition + 2).
                Buffer.BlockCopy(new byte[16], 0, tempPacket, messageAuthenticatorPosition + 2, 16);

                var calculatedMessageAuthenticator = CalculateMessageAuthenticator(tempPacket, sharedSecret, requestAuthenticator);
                if (!ArraysAreEqual(calculatedMessageAuthenticator, messageAuthenticator))
                {
                    throw new InvalidOperationException($"Invalid Message-Authenticator in packet {packet.Header.Identifier}");
                }
            }

            packet.RequestAuthenticator = requestAuthenticator;

            return packet;
        }


        /// <summary>
        /// Parses the content and returns an object of proper type
        /// </summary>
        /// <param name="contentBytes"></param>
        /// <param name="type"></param>
        /// <param name="code"></param>
        /// <param name="authenticator"></param>
        /// <param name="sharedSecret"></param>
        /// <returns></returns>
        private static object ParseContentBytes(byte[] contentBytes, string type, uint code, RadiusAuthenticator authenticator, SharedSecret sharedSecret)
        {
            switch (type)
            {
                case DictionaryAttribute.TYPE_STRING:
                case DictionaryAttribute.TYPE_TAGGED_STRING:
                    //couse some NAS (like NPS) send binary within string attributes, check content before unpack to prevent data loss
                    if (contentBytes.All(b => b >= 32 && b <= 127)) //only if ascii
                    {
                        return Encoding.UTF8.GetString(contentBytes);
                    }
                    return contentBytes;

                case DictionaryAttribute.TYPE_OCTET:
                    // If this is a password attribute it must be decrypted
                    if (code == RadiusAttributeCode.UserPassword)
                    {
                        return RadiusPassword.Decrypt(sharedSecret, authenticator, contentBytes);
                    }
                    return contentBytes;

                case DictionaryAttribute.TYPE_INTEGER:
                case DictionaryAttribute.TYPE_TAGGED_INTEGER:
                    return BitConverter.ToUInt32(contentBytes.Reverse().ToArray(), 0);

                case DictionaryAttribute.TYPE_IPADDR:
                    return new IPAddress(contentBytes);

                default:
                    return null;
            }
        }


        /// <summary>
        /// Validates a message authenticator attribute if one exists in the packet
        /// Message-Authenticator = HMAC-MD5 (Type, Identifier, Length, Request Authenticator, Attributes)
        /// The HMAC-MD5 function takes in two arguments:
        /// The payload of the packet, which includes the 16 byte Message-Authenticator field filled with zeros
        /// The shared secret
        /// https://www.ietf.org/rfc/rfc2869.txt
        /// </summary>
        /// <returns></returns>
        private static byte[] CalculateMessageAuthenticator(byte[] packetBytes, SharedSecret sharedSecret, byte[] requestAuthenticator)
        {
            var temp = new byte[packetBytes.Length];
            packetBytes.CopyTo(temp, 0);

            requestAuthenticator?.CopyTo(temp, 4);

            using var md5 = new HMACMD5(sharedSecret.Bytes);
            return md5.ComputeHash(temp);
        }


        /// <summary>
        /// Creates a response authenticator
        /// Response authenticator = MD5(Code+ID+Length+RequestAuth+Attributes+Secret)
        /// Actually this means it is the response packet with the request authenticator and secret...
        /// </summary>
        /// <param name="sharedSecret"></param>
        /// <param name="requestAuthenticator"></param>
        /// <param name="packetBytes"></param>
        /// <returns>Response authenticator for the packet</returns>
        private static byte[] CalculateResponseAuthenticator(SharedSecret sharedSecret, byte[] requestAuthenticator, byte[] packetBytes)
        {
            var responseAuthenticator = packetBytes.Concat(sharedSecret.Bytes).ToArray();
            Buffer.BlockCopy(requestAuthenticator, 0, responseAuthenticator, 4, 16);

            using var md5 = MD5.Create();
            return md5.ComputeHash(responseAuthenticator);
        }

        /// <summary>
        /// Calculate the request authenticator used in accounting, disconnect and coa requests
        /// </summary>
        /// <param name="sharedSecret"></param>
        /// <param name="packetBytes"></param>
        /// <returns></returns>
        internal static byte[] CalculateRequestAuthenticator(SharedSecret sharedSecret, byte[] packetBytes)
        {
            return CalculateResponseAuthenticator(sharedSecret, new byte[16], packetBytes);
        }

        /// <summary>
        /// Get the raw packet bytes
        /// </summary>
        /// <returns></returns>
        public byte[] GetBytes(IRadiusPacket packet)
        {
            var packetBytes = new List<byte>
            {
                (byte)packet.Header.Code,
                packet.Header.Identifier
            };
            packetBytes.AddRange(new byte[18]); // Placeholder for length and authenticator

            var messageAuthenticatorPosition = 0;
            foreach (var attribute in packet.Attributes)
            {
                // todo add logic to check attribute object type matches type in dictionary?
                foreach (var value in attribute.Value)
                {
                    var contentBytes = GetAttributeValueBytes(value);
                    var headerBytes = new byte[2];

                    var attributeType = _radiusDictionary.GetAttribute(attribute.Key);
                    switch (attributeType)
                    {
                        case DictionaryVendorAttribute _attributeType:
                            headerBytes = new byte[8];
                            headerBytes[0] = RadiusAttributeCode.VendorSpecific; // VSA type

                            var vendorId = BitConverter.GetBytes(_attributeType.VendorId);
                            Array.Reverse(vendorId);
                            Buffer.BlockCopy(vendorId, 0, headerBytes, 2, 4);
                            headerBytes[6] = (byte)_attributeType.VendorCode;
                            headerBytes[7] = (byte)(2 + contentBytes.Length);  // length of the vsa part
                            break;

                        case DictionaryAttribute _attributeType:
                            headerBytes[0] = attributeType.Code;

                            // Encrypt password if this is a User-Password attribute
                            if (_attributeType.Code == RadiusAttributeCode.UserPassword)
                            {
                                contentBytes = RadiusPassword.Encrypt(packet.SharedSecret, packet.Authenticator, contentBytes);
                            }
                            else if (_attributeType.Code == RadiusAttributeCode.MessageAuthenticator)    // Remember the position of the message authenticator, because it has to be added after everything else
                            {
                                messageAuthenticatorPosition = packetBytes.Count;
                            }
                            break;

                        default:
                            throw new InvalidOperationException("Unknown attribute {attribute.Key}, check spelling or dictionary");
                    }

                    headerBytes[1] = (byte)(headerBytes.Length + contentBytes.Length);
                    packetBytes.AddRange(headerBytes);
                    packetBytes.AddRange(contentBytes);
                }
            }

            // Note the order of the bytes...
            var packetLengthBytes = BitConverter.GetBytes(packetBytes.Count);
            packetBytes[2] = packetLengthBytes[1];
            packetBytes[3] = packetLengthBytes[0];

            var packetBytesArray = packetBytes.ToArray();

            if (packet.Header.Code == PacketCode.AccountingRequest || packet.Header.Code == PacketCode.DisconnectRequest || packet.Header.Code == PacketCode.CoaRequest)
            {
                if (messageAuthenticatorPosition != 0)
                {
                    var temp = new byte[16];
                    Buffer.BlockCopy(temp, 0, packetBytesArray, messageAuthenticatorPosition + 2, 16);
                    var messageAuthenticatorBytes = CalculateMessageAuthenticator(packetBytesArray, packet.SharedSecret, null);
                    Buffer.BlockCopy(messageAuthenticatorBytes, 0, packetBytesArray, messageAuthenticatorPosition + 2, 16);
                }

                var authenticator = CalculateRequestAuthenticator(packet.SharedSecret, packetBytesArray);
                Buffer.BlockCopy(authenticator, 0, packetBytesArray, 4, 16);
            }
            else if (packet.Header.Code == PacketCode.StatusServer)
            {
                var authenticator = packet.RequestAuthenticator != null 
                    ? CalculateResponseAuthenticator(packet.SharedSecret, packet.RequestAuthenticator, packetBytesArray) 
                    : packet.Authenticator.Value;
                Buffer.BlockCopy(authenticator, 0, packetBytesArray, 4, 16);

                if (messageAuthenticatorPosition != 0)
                {
                    var temp = new byte[16];
                    Buffer.BlockCopy(temp, 0, packetBytesArray, messageAuthenticatorPosition + 2, 16);
                    var messageAuthenticatorBytes = CalculateMessageAuthenticator(packetBytesArray, packet.SharedSecret, packet.RequestAuthenticator);
                    Buffer.BlockCopy(messageAuthenticatorBytes, 0, packetBytesArray, messageAuthenticatorPosition + 2, 16);
                }
            }
            else
            {
                if (packet.RequestAuthenticator == null)
                {
                    Buffer.BlockCopy(packet.Authenticator.Value, 0, packetBytesArray, 4, 16);
                }

                if (messageAuthenticatorPosition != 0)
                {
                    var temp = new byte[16];
                    Buffer.BlockCopy(temp, 0, packetBytesArray, messageAuthenticatorPosition + 2, 16);
                    var messageAuthenticatorBytes = CalculateMessageAuthenticator(packetBytesArray, packet.SharedSecret, packet.RequestAuthenticator);
                    Buffer.BlockCopy(messageAuthenticatorBytes, 0, packetBytesArray, messageAuthenticatorPosition + 2, 16);
                }

                if (packet.RequestAuthenticator != null)
                {
                    var authenticator = CalculateResponseAuthenticator(packet.SharedSecret, packet.RequestAuthenticator, packetBytesArray);
                    Buffer.BlockCopy(authenticator, 0, packetBytesArray, 4, 16);
                }
            }

            return packetBytesArray;
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
                case string _value:
                    return Encoding.UTF8.GetBytes(_value);

                case uint _value:
                    var contentBytes = BitConverter.GetBytes(_value);
                    Array.Reverse(contentBytes);
                    return contentBytes;

                case byte[] _value:
                    return _value;

                case IPAddress _value:
                    return _value.GetAddressBytes();

                default:
                    throw new NotImplementedException();
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

        private static bool ArraysAreEqual(byte[] firstArray, byte[] secondArray)
        {
            if (firstArray.Length != secondArray.Length)
            {
                return false;
            }

            for (int i = 0; i < firstArray.Length; i++)
            {
                if (firstArray[i] != secondArray[i])
                {
                    return false;
                }
            }
            return true;
        }
    }
}
