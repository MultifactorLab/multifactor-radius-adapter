using System.Globalization;
using System.Net;
using Multifactor.Radius.Adapter.v2.Domain.Radius.Attributes;
using Multifactor.Radius.Adapter.v2.Infrastructure.Radius.Interfaces;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Radius;

public class RadiusAttributeTypeConverter : IRadiusAttributeTypeConverter
{
    private readonly IRadiusDictionary _dictionary;

    public RadiusAttributeTypeConverter(IRadiusDictionary dictionary)
    {
        _dictionary = dictionary;
    }

    public object ConvertType(string attributeName, object value)
    {
        if (value is not string stringValue)
            return value;

        var attribute = _dictionary.GetAttribute(attributeName);
        if (attribute == null)
            return value;

        return attribute.Type switch
        {
            "ipaddr" => ConvertToIpAddress(stringValue),
            "date" => ConvertToDateTime(stringValue),
            "integer" => ConvertToInteger(stringValue),
            _ => value
        };
    }

    private static object ConvertToIpAddress(string value)
    {
        if (IPAddress.TryParse(value, out var ipAddress))
            return ipAddress;

        if (int.TryParse(value, out var intValue))
            return ConvertMsRadiusFramedIpAddress(intValue);

        return value;
    }

    private static object ConvertToDateTime(string value)
    {
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date))
            return date;

        return value;
    }

    private static object ConvertToInteger(string value)
    {
        if (int.TryParse(value, out var integer))
            return integer;

        return value;
    }

    private static IPAddress ConvertMsRadiusFramedIpAddress(int intValue)
    {
        const long adjustment = 4294967296L;

        var longValue = (long)intValue;

        if (intValue < 0)
        {
            longValue += adjustment;
        }

        var bytes = BitConverter.GetBytes(longValue).Take(4).ToArray();
        
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        return new IPAddress(bytes);
    }
}