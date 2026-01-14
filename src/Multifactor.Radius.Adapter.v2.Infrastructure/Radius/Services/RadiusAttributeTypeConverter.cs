using System.Globalization;
using System.Net;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Services;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Dictionary;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Radius.Services;

public class RadiusAttributeTypeConverter : IRadiusAttributeTypeConverter
{
    private readonly IRadiusDictionary _radiusDictionary;
    
    public RadiusAttributeTypeConverter(IRadiusDictionary radiusDictionary)
    {
        _radiusDictionary = radiusDictionary ?? throw new ArgumentNullException(nameof(radiusDictionary));
    }
    
    public object ConvertType(string attributeName, object value)
    {
        // Если значение не строка - возвращаем как есть
        if (value is not string stringValue)
            return value;
            
        // Получаем информацию об атрибуте из словаря
        var attributeInfo = _radiusDictionary.GetAttribute(attributeName);
        if (attributeInfo == null)
        {
            // Неизвестный атрибут - возвращаем как есть
            return value;
        }
        
        return ConvertStringToType(stringValue, attributeInfo.Type);
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
            _ => stringValue // Неподдерживаемый тип - возвращаем как строку
        };
    }
    
    private object ConvertToIpAddress(string stringValue)
    {
        // Пробуем парсить как обычный IP-адрес
        if (IPAddress.TryParse(stringValue, out var ipAddress))
            return ipAddress;
            
        // Пробуем парсить как Microsoft RADIUS Framed IP Address (целое число)
        if (int.TryParse(stringValue, out var intValue))
            return ConvertMsRadiusFramedIpAddress(intValue);
            
        // Не удалось конвертировать - возвращаем исходную строку
        return stringValue;
    }
    
    private IPAddress ConvertMsRadiusFramedIpAddress(int intValue)
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
    
    private object ConvertToDateTime(string stringValue)
    {
        // Пробуем парсить как DateTime
        if (DateTime.TryParse(stringValue, CultureInfo.InvariantCulture, 
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, 
            out var dateTime))
        {
            return dateTime;
        }
        
        // Пробуем парсить как Unix timestamp
        if (long.TryParse(stringValue, out var unixTimestamp))
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).UtcDateTime;
        }
        
        return stringValue;
    }
    
    private object ConvertToInteger(string stringValue)
    {
        if (int.TryParse(stringValue, out var intValue))
            return intValue;
            
        return stringValue;
    }
    
    private object ConvertToOctets(string stringValue)
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
            // Если не удалось - возвращаем как строку
        }
        
        return stringValue;
    }
    
    private static byte[] HexStringToByteArray(string hex)
    {
        var bytes = new byte[hex.Length / 2];
        for (int i = 0; i < hex.Length; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        }
        return bytes;
    }
}