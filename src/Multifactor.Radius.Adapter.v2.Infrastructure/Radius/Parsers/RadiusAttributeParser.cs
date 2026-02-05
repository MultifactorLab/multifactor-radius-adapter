using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Models.Dictionary;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Models.Dictionary.Attributes;
using Multifactor.Radius.Adapter.v2.Infrastructure.Radius.Crypto;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Radius.Parsers;

public class RadiusAttributeParser : IRadiusAttributeParser
{
    private readonly IRadiusDictionary _radiusDictionary;
    private readonly ILogger<RadiusAttributeParser> _logger;
    const int VendorSpecific = 26;
    const int MessageAuthenticator = 80;
    const int UserPassword = 2;

    public RadiusAttributeParser(
        IRadiusDictionary radiusDictionary,
        ILogger<RadiusAttributeParser> logger)
    {
        _radiusDictionary = radiusDictionary ?? throw new ArgumentNullException(nameof(radiusDictionary));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ParsedAttribute? Parse(byte[] attributeData, byte typeCode, RadiusAuthenticator authenticator, SharedSecret sharedSecret)
    {
        try
        {
            if (typeCode == VendorSpecific) // Vendor-Specific
            {
                return ParseVendorSpecificAttribute(attributeData, authenticator, sharedSecret);
            }
            return ParseStandardAttribute(typeCode, attributeData, authenticator, sharedSecret);
            
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to parse attribute type {TypeCode}", typeCode);
            return null;
        }
    }

    private ParsedAttribute? ParseVendorSpecificAttribute(
        byte[] contentBytes,
        RadiusAuthenticator authenticator,
        SharedSecret sharedSecret)
    {
        if (contentBytes.Length < 6)
            return null;

        byte[] vendorIdBytes = new byte[4];
        Buffer.BlockCopy(contentBytes, 0, vendorIdBytes, 0, 4);
        Array.Reverse(vendorIdBytes);
        uint vendorId = BitConverter.ToUInt32(vendorIdBytes, 0);

        byte vendorType = contentBytes[4];
        byte vendorLength = contentBytes[5];

        if (vendorLength < 2 || 2 + vendorLength - 2 > contentBytes.Length)
            return null;

        byte[] vendorContentBytes = new byte[vendorLength - 2];
        Buffer.BlockCopy(contentBytes, 6, vendorContentBytes, 0, vendorContentBytes.Length);

        var vendorAttribute = _radiusDictionary.GetVendorAttribute(vendorId, vendorType);
        if (vendorAttribute == null)
        {
            _logger.LogDebug("Unknown VSA: VendorId={VendorId}, VendorType={VendorType}", vendorId, vendorType);
            return null;
        }

        var content = ParseContentBytes(
            vendorContentBytes,
            vendorAttribute.Type,
            26,
            authenticator,
            sharedSecret);

        if (content == null)
            return null;

        return new ParsedAttribute(vendorAttribute.Name, content, false);
    }

    private ParsedAttribute? ParseStandardAttribute(
        byte typeCode,
        byte[] contentBytes,
        RadiusAuthenticator authenticator,
        SharedSecret sharedSecret)
    {
        var attributeDefinition = _radiusDictionary.GetAttribute(typeCode);
        if (attributeDefinition == null)
        {
            _logger.LogDebug("Unknown attribute type: {TypeCode}", typeCode);
            return null;
        }

        var content = ParseContentBytes(
            contentBytes,
            attributeDefinition.Type,
            typeCode,
            authenticator,
            sharedSecret);

        if (content == null)
            return null;

        bool isMessageAuthenticator = attributeDefinition.Code == MessageAuthenticator;
        
        return new ParsedAttribute(attributeDefinition.Name, content, isMessageAuthenticator);
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
                if (code == UserPassword)
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
}