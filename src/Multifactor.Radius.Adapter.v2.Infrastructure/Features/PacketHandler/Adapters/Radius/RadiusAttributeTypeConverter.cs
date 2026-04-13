using System.Globalization;
using System.Net;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Models.Dictionary;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.Adapters.Radius;

internal interface IRadiusAttributeTypeConverter
{
    object ConvertType(string attributeName, object value);
}

internal sealed class RadiusAttributeTypeConverter : IRadiusAttributeTypeConverter
{
    private readonly IRadiusDictionary _radiusDictionary;
    public RadiusAttributeTypeConverter(IRadiusDictionary radiusDictionary)
    {
        _radiusDictionary = radiusDictionary ?? throw new ArgumentNullException(nameof(radiusDictionary));
    }
    
    public object ConvertType(string attributeName, object value)
    {
        if (value is not string stringValue)
            return value;
            
        var attributeInfo = _radiusDictionary.GetAttribute(attributeName);
        return attributeInfo == null ? value : ConvertStringToType(stringValue, attributeInfo.Type);
    }
    
    private object ConvertStringToType(string stringValue, string attributeType)
    {
        return attributeType.ToLowerInvariant() switch
        {
            "ipaddr" => ConvertToIpAddress(stringValue),
            "date" => ConvertToDateTime(stringValue),
            "integer" => ConvertToInteger(stringValue),
            "string" or "tagged-string" => stringValue,
            "octets" => ConvertToOctets(stringValue),
            _ => stringValue
        };
    }
    
    private static object ConvertToIpAddress(string stringValue)
    {
        if (IPAddress.TryParse(stringValue, out var ipAddress))
            return ipAddress;
            
        if (int.TryParse(stringValue, out var intValue))
            return ConvertMsRadiusFramedIpAddress(intValue);
            
        return stringValue;
    }
    
    private static IPAddress ConvertMsRadiusFramedIpAddress(int intValue)
    {
        // Microsoft RADIUS специфика:
        // Числа выше 2147483647 представляются как отрицательные
        long longValue = intValue;
        
        if (longValue < 0)
        {
            // Конвертируем negative int в unsigned long
            longValue += 4294967296L; // 2^32
        }
        
        // Конвертируем в байты (big-endian для IP-адреса)
        var bytes = BitConverter.GetBytes(longValue);
        
        // Берем только первые 4 байта (IPv4)
        var ipBytes = new byte[4];
        Array.Copy(bytes, 0, ipBytes, 0, 4);
        
        // Конвертируем big-endian если нужно
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(ipBytes);
        }
        return new IPAddress(ipBytes);
    }
    
    private static object ConvertToDateTime(string stringValue)
    {
        if (DateTime.TryParse(stringValue, CultureInfo.InvariantCulture, 
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, 
            out var dateTime))
        {
            return dateTime;
        }
        
        if (long.TryParse(stringValue, out var unixTimestamp))
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).UtcDateTime;
        }
        
        return stringValue;
    }
    
    private static object ConvertToInteger(string stringValue)
    {
        if (int.TryParse(stringValue, out var intValue))
            return intValue;
            
        return stringValue;
    }
    
    private static object ConvertToOctets(string stringValue)
    {
        // Для octets можно конвертировать из hex или base64
        try
        {
            // Пробуем как hex строку
            if (stringValue.Length % 2 == 0 && 
                stringValue.All(c => char.IsDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
            {
                return HexStringToByteArray(stringValue);
            }
            
            // Пробуем как base64
            if (stringValue.Length % 4 == 0 && 
                stringValue.All(c => char.IsLetterOrDigit(c) || c == '+' || c == '/' || c == '='))
            {
                return Convert.FromBase64String(stringValue);
            }
        }
        catch
        {
            return stringValue;
        }
        return stringValue;
    }
    
    private static byte[] HexStringToByteArray(string hex)
    {
        var bytes = new byte[hex.Length / 2];
        for (var i = 0; i < hex.Length; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        }
        return bytes;
    }
}