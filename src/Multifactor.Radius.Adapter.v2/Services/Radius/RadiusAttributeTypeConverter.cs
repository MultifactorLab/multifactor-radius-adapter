using System.Globalization;
using System.Net;
using Multifactor.Radius.Adapter.v2.Core.Radius.Attributes;

namespace Multifactor.Radius.Adapter.v2.Services.Radius;

public class RadiusAttributeTypeConverter : IRadiusAttributeTypeConverter
{
    private readonly IRadiusDictionary _dictionary;

    public RadiusAttributeTypeConverter(IRadiusDictionary dictionary)
    {
        _dictionary = dictionary;
    }

    public object ConvertType(string attrName, object value)
    {
        if (value is not string stringValue)
            return value;
        
        var attribute = _dictionary.GetAttribute(attrName);
        switch (attribute.Type)
        {
            case "ipaddr":
                if (IPAddress.TryParse(stringValue, out var ipValue))
                    return ipValue;
                    
                // maybe it is msRADIUSFramedIPAddress value
                if (int.TryParse(stringValue, out var val))
                    return MsRadiusFramedIpAddressToIpAddress(val);
                    
                break;
            case "date":
                if (DateTime.TryParse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dateValue))
                    return dateValue;
                break;
            case "integer":
                if (int.TryParse(stringValue, out var integerValue))
                    return integerValue;
                break;
        }

        return value;
    }
    
    private IPAddress MsRadiusFramedIpAddressToIpAddress(int intValue)
    {
        long longValue = intValue;
            
        // Microsoft subtracts 4294967296 from numbers above 2147483647 to
        // make them negative to make it, sort of, unsigned.
        // https://document.phenixid.net/m/90910/l/1601121-how-to-setup-framed-ip-using-ad-with-msradiusframedipaddress-attribute
        if (longValue < 0)
        {
            longValue += 4294967296;
        }
            
        var bytes = BitConverter.GetBytes(longValue).Take(4).ToArray();
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        return new IPAddress(bytes);
    }
}