using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;
using Multifactor.Radius.Adapter.v2.Application.Security;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Dictionary;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Dictionary.Attributes;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Radius.Builders;

public interface IRadiusAttributeSerializer
{
    byte[]? Serialize(string attributeName, object value, RadiusAuthenticator authenticator, SharedSecret sharedSecret, RadiusAuthenticator? requestAuthenticator = null);
}

public class RadiusAttributeSerializer : IRadiusAttributeSerializer
{
    private readonly IRadiusDictionary _radiusDictionary;
    private readonly ILogger<RadiusAttributeSerializer> _logger;

    public RadiusAttributeSerializer(
        IRadiusDictionary radiusDictionary,
        ILogger<RadiusAttributeSerializer> logger)
    {
        _radiusDictionary = radiusDictionary;
        _logger = logger;
    }

    public byte[]? Serialize(string attributeName, object value, RadiusAuthenticator authenticator, SharedSecret sharedSecret, RadiusAuthenticator? requestAuthenticator = null)
    {
        try
        {
            var attributeDefinition = _radiusDictionary.GetAttribute(attributeName);
            if (attributeDefinition == null)
            {
                _logger.LogWarning("Unknown attribute: {AttributeName}", attributeName);
                return null;
            }

            byte[] contentBytes = ConvertValueToBytes(value, attributeDefinition.Type);
            
            // Special handling for certain attributes
            if (attributeDefinition.Code == 2) // User-Password
            {
                contentBytes = RadiusPasswordProtector.Encrypt(sharedSecret, authenticator, contentBytes);
            }
            else if (attributeDefinition.Code == 80) // Message-Authenticator
            {
                // Will be calculated later, fill with zeros for now
                contentBytes = new byte[16];
            }

            byte[] headerBytes;
            if (attributeDefinition is DictionaryVendorAttribute vendorAttribute)
            {
                headerBytes = CreateVendorSpecificHeader(vendorAttribute, contentBytes.Length);
            }
            else
            {
                headerBytes = CreateStandardHeader(attributeDefinition.Code, contentBytes.Length);
            }

            var result = new byte[headerBytes.Length + contentBytes.Length];
            Buffer.BlockCopy(headerBytes, 0, result, 0, headerBytes.Length);
            Buffer.BlockCopy(contentBytes, 0, result, headerBytes.Length, contentBytes.Length);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to serialize attribute: {AttributeName}", attributeName);
            return null;
        }
    }

    private byte[] ConvertValueToBytes(object value, string type)
    {
        switch (type.ToLowerInvariant())
        {
            case "string":
            case "tagged-string":
                return Encoding.UTF8.GetBytes(value.ToString() ?? string.Empty);
                
            case "octets":
                if (value is byte[] bytes)
                    return bytes;
                if (value is string str)
                    return Encoding.UTF8.GetBytes(str);
                throw new ArgumentException($"Cannot convert {value.GetType()} to octets");
                
            case "integer":
            case "tagged-integer":
                return ConvertIntegerToBytes(value);
                
            case "ipaddr":
                if (value is IPAddress ip)
                    return ip.GetAddressBytes();
                if (value is string ipStr)
                    return IPAddress.Parse(ipStr).GetAddressBytes();
                throw new ArgumentException($"Cannot convert {value.GetType()} to IP address");
                
            case "date":
                return ConvertDateToBytes(value);
                
            default:
                _logger.LogWarning("Unknown attribute type: {Type}", type);
                if (value is byte[] byteArray)
                    return byteArray;
                return Encoding.UTF8.GetBytes(value.ToString() ?? string.Empty);
        }
    }

    private byte[] ConvertIntegerToBytes(object value)
    {
        int intValue;
        
        switch (value)
        {
            case int i:
                intValue = i;
                break;
            case uint ui:
                intValue = (int)ui;
                break;
            case short s:
                intValue = s;
                break;
            case ushort us:
                intValue = us;
                break;
            case byte b:
                intValue = b;
                break;
            case string str when int.TryParse(str, out int parsed):
                intValue = parsed;
                break;
            default:
                throw new ArgumentException($"Cannot convert {value.GetType()} to integer");
        }

        var bytes = BitConverter.GetBytes(intValue);
        Array.Reverse(bytes);
        return bytes;
    }

    private byte[] ConvertDateToBytes(object value)
    {
        DateTime date;
        
        if (value is DateTime dt)
        {
            date = dt;
        }
        else if (value is string str && DateTime.TryParse(str, out var parsed))
        {
            date = parsed;
        }
        else
        {
            date = DateTime.UtcNow;
        }

        var unixTime = (uint)(date - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        var bytes = BitConverter.GetBytes(unixTime);
        Array.Reverse(bytes);
        return bytes;
    }

    private byte[] CreateStandardHeader(byte typeCode, int contentLength)
    {
        var header = new byte[2];
        header[0] = typeCode;
        header[1] = (byte)(2 + contentLength); // Total length: header (2) + content
        return header;
    }

    private byte[] CreateVendorSpecificHeader(DictionaryVendorAttribute vendorAttribute, int contentLength)
    {
        // VSA format: Type(1)=26, Length(1), Vendor-Id(4), Vendor-Type(1), Vendor-Length(1), Content
        var header = new byte[8];
        
        header[0] = 26; // Vendor-Specific attribute type
        header[1] = (byte)(8 + contentLength); // Total VSA length
        
        var vendorIdBytes = BitConverter.GetBytes(vendorAttribute.VendorId);
        Array.Reverse(vendorIdBytes);
        Buffer.BlockCopy(vendorIdBytes, 0, header, 2, 4);
        
        header[6] = (byte)vendorAttribute.VendorCode;
        header[7] = (byte)(2 + contentLength); // Vendor-specific part length
        
        return header;
    }
}