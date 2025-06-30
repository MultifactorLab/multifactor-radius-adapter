using System.Globalization;
using System.Net;
using Moq;
using Multifactor.Radius.Adapter.v2.Core.Radius.Attributes;
using Multifactor.Radius.Adapter.v2.Services.Radius;

namespace Multifactor.Radius.Adapter.v2.Tests.Unit;

public class RadiusAttributeTypeConverterTests
{
    [Fact]
    public void ConvertString()
    {
        var radiusDictionaryMock = new Mock<IRadiusDictionary>();
        radiusDictionaryMock.Setup(x => x.GetAttribute("key")).Returns(new DictionaryAttribute("key", 1, "string"));
        var converter = new RadiusAttributeTypeConverter(radiusDictionaryMock.Object);
        var radiusAttribute = converter.ConvertType("key", "string");
        Assert.NotNull(radiusAttribute);
        Assert.Equal("string", radiusAttribute as string);
    }
    
    [Fact]
    public void ConvertIpaddr()
    {
        var radiusDictionaryMock = new Mock<IRadiusDictionary>();
        radiusDictionaryMock.Setup(x => x.GetAttribute("key")).Returns(new DictionaryAttribute("key", 1, "ipaddr"));
        var converter = new RadiusAttributeTypeConverter(radiusDictionaryMock.Object);
        var radiusAttribute = converter.ConvertType("key", "127.0.0.1");
        Assert.NotNull(radiusAttribute);
        Assert.Equal(IPAddress.Parse("127.0.0.1"), radiusAttribute as IPAddress);
    }
    
    [Fact]
    public void ConvertDate()
    {
        var radiusDictionaryMock = new Mock<IRadiusDictionary>();
        radiusDictionaryMock.Setup(x => x.GetAttribute("key")).Returns(new DictionaryAttribute("key", 1, "date"));
        var converter = new RadiusAttributeTypeConverter(radiusDictionaryMock.Object);
        DateTime.TryParse("12.10.2025", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dateValue);
        var radiusAttribute = converter.ConvertType("key", dateValue.Date.ToString(CultureInfo.InvariantCulture));
        
        Assert.NotNull(radiusAttribute);
        Assert.Equal(dateValue, radiusAttribute);
    }
    
    [Fact]
    public void ConvertInteger()
    {
        var radiusDictionaryMock = new Mock<IRadiusDictionary>();
        radiusDictionaryMock.Setup(x => x.GetAttribute("key")).Returns(new DictionaryAttribute("key", 1, "integer"));
        var converter = new RadiusAttributeTypeConverter(radiusDictionaryMock.Object);
        var radiusAttribute = converter.ConvertType("key", 123);
        
        Assert.NotNull(radiusAttribute);
        Assert.Equal(123, radiusAttribute);
    }
    
    [Fact]
    public void ConvertMsRadiusFramedIpAddress()
    {
        var radiusDictionaryMock = new Mock<IRadiusDictionary>();
        radiusDictionaryMock.Setup(x => x.GetAttribute("key")).Returns(new DictionaryAttribute("key", 1, "ipaddr"));
        var converter = new RadiusAttributeTypeConverter(radiusDictionaryMock.Object);
        var radiusAttribute = converter.ConvertType("key", "-1235");
        
        Assert.NotNull(radiusAttribute);
        Assert.Equal(IPAddress.Parse("255.255.251.45"), radiusAttribute as IPAddress);
    }
}