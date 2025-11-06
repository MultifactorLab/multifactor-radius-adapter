using Multifactor.Radius.Adapter.v2.Core.Radius.Packet;
using Multifactor.Radius.Adapter.v2.Tests.Fixture;

namespace Multifactor.Radius.Adapter.v2.Tests.Radius;

public class RadiusAttributeTests
{
    [Fact]
    public void CreateDefaultRadiusAttribute_ShouldCreate()
    {
        var attribute = new RadiusAttribute("name");
        Assert.Equal("name", attribute.Name);
        Assert.Empty(attribute.Values);
    }
    
    [Fact]
    public void AddAttributeValue_NoValue_ShouldThrow()
    {
        var attribute = new RadiusAttribute("name");

        Assert.Throws<ArgumentException>(() => attribute.AddValues());
        Assert.Empty(attribute.Values);
    }
    
    [Theory]
    [ClassData(typeof(EmptyStringsListInput))]
    public void AddAttributeValue_EmptyValue_ShouldAdd(object value)
    {
        var attribute = new RadiusAttribute("name");

        attribute.AddValues(value);
        Assert.Single(attribute.Values);
        var val = attribute.Values[0];
        Assert.Equal(value, val);
    }
    
    [Fact]
    public void AddAttributeValue_ShouldAdd()
    {
        var attribute = new RadiusAttribute("name");
        var value = "value";
        attribute.AddValues(value);
        Assert.Single(attribute.Values);
        Assert.Equal(value, attribute.Values[0]);
    }
    
    [Fact]
    public void AddAttributeValues_ShouldAddTwoValues()
    {
        var attribute = new RadiusAttribute("name");
        var value1 = "value1";
        var value2 = "value2";
        
        attribute.AddValues(value1);
        attribute.AddValues(value2);
        Assert.Equal(2, attribute.Values.Count);
        Assert.Collection(
            attribute.Values,
            e => Assert.Equal(value1, e),
            e => Assert.Equal(value2, e));
    }

    [Fact]
    public void RemoveAttributeValues_ShouldRemove()
    {
        var attribute = new RadiusAttribute("name");
        var value1 = "value1";
        var value2 = "value2";
        
        attribute.AddValues(value1);
        attribute.AddValues(value2);
        
        attribute.RemoveAllValues();
        Assert.Empty(attribute.Values);
    }
}