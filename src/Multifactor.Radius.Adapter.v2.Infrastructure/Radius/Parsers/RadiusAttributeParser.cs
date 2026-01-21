using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Dictionary;
using Multifactor.Radius.Adapter.v2.Infrastructure.Radius.Crypto;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Radius.Parsers;

public class RadiusAttributeParser : IRadiusAttributeParser
{
    private readonly IRadiusDictionary _radiusDictionary;
    private readonly IRadiusCryptoProvider _cryptoProvider;
    private readonly ILogger<RadiusAttributeParser> _logger;

    public RadiusAttributeParser(
        IRadiusDictionary radiusDictionary,
        IRadiusCryptoProvider cryptoProvider,
        ILogger<RadiusAttributeParser> logger)
    {
        _radiusDictionary = radiusDictionary ?? throw new ArgumentNullException(nameof(radiusDictionary));
        _cryptoProvider = cryptoProvider ?? throw new ArgumentNullException(nameof(cryptoProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ParsedAttribute? Parse(byte[] attributeData, RadiusAuthenticator authenticator, SharedSecret sharedSecret)
    {
        if (attributeData == null || attributeData.Length < 2)
            return null;

        byte typeCode = attributeData[0];
        byte length = attributeData[1];

        if (length > attributeData.Length)
            return null;

        byte[] contentBytes = new byte[length - 2];
        if (contentBytes.Length > 0)
        {
            Buffer.BlockCopy(attributeData, 2, contentBytes, 0, contentBytes.Length);
        }

        try
        {
            if (typeCode == 26) // Vendor-Specific
            {
                return ParseVendorSpecificAttribute(contentBytes, authenticator, sharedSecret);
            }
            else
            {
                return ParseStandardAttribute(typeCode, contentBytes, authenticator, sharedSecret);
            }
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

        bool isMessageAuthenticator = attributeDefinition.Code == 80; // Message-Authenticator
        
        return new ParsedAttribute(attributeDefinition.Name, content, isMessageAuthenticator);
    }

    private object ParseContentBytes(
        byte[] contentBytes,
        string type,
        uint code,
        RadiusAuthenticator authenticator,
        SharedSecret sharedSecret)
    {
        switch (type.ToLowerInvariant())
        {
            case "string":
            case "tagged-string":
                return ParseString(contentBytes);
                
            case "octets":
                if (code == 2) // User-Password
                {
                    return _cryptoProvider.DecryptPassword(sharedSecret, authenticator, contentBytes);
                }
                return contentBytes;
                
            case "integer":
            case "tagged-integer":
                return ParseInteger(contentBytes);
                
            case "ipaddr":
                return ParseIpAddress(contentBytes);
                
            case "date":
                return ParseDate(contentBytes);
                
            case "ifid":
                return contentBytes;
                
            default:
                _logger.LogWarning("Unknown attribute type: {Type}", type);
                return contentBytes;
        }
    }

    private static string ParseString(byte[] bytes)
    {
        // Try to decode as UTF-8, fall back to ASCII if invalid
        try
        {
            return Encoding.UTF8.GetString(bytes).TrimEnd('\0');
        }
        catch
        {
            return Encoding.ASCII.GetString(bytes).TrimEnd('\0');
        }
    }

    private static int ParseInteger(byte[] bytes)
    {
        switch (bytes.Length)
        {
            case 4:
                Array.Reverse(bytes);
                return BitConverter.ToInt32(bytes, 0);
            case 2:
                Array.Reverse(bytes);
                return BitConverter.ToInt16(bytes, 0);
            case 1:
                return bytes[0];
            default:
                throw new InvalidOperationException($"Invalid integer length: {bytes.Length}");
        }
    }

    private static IPAddress ParseIpAddress(byte[] bytes)
    {
        return new IPAddress(bytes);
    }

    private static DateTime ParseDate(byte[] bytes)
    {
        Array.Reverse(bytes);
        uint seconds = BitConverter.ToUInt32(bytes, 0);
        return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(seconds);
    }
}