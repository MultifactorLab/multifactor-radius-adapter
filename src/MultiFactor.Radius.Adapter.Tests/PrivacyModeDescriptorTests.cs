using FluentAssertions;
using MultiFactor.Radius.Adapter.Configuration.Features.PrivacyModeFeature;

namespace MultiFactor.Radius.Adapter.Tests;

[Trait("Category", "Privacy Mode")]
public class PrivacyModeDescriptorTests
{
    [Theory]
    [InlineData("Full", PrivacyMode.Full)]
    [InlineData("full", PrivacyMode.Full)]
    [InlineData("None", PrivacyMode.None)]
    [InlineData("none", PrivacyMode.None)]
    [InlineData("", PrivacyMode.None)]
    [InlineData(" ", PrivacyMode.None)]
    [InlineData(null, PrivacyMode.None)]
    public void Create_ValidValues_ShouldReturnInstance(string value, PrivacyMode expected)
    {
        var des = PrivacyModeDescriptor.Create(value);

        des.Should().NotBeNull();
        des.Mode.Should().Be(expected);
    }

    [Theory]
    [InlineData("Partial")]
    [InlineData("partial")]
    [InlineData("Partial:")]
    [InlineData("partial:Field1,field2")]
    public void Create_ShouldReturnPartial(string value)
    {
        var des = PrivacyModeDescriptor.Create(value);

        des.Should().NotBeNull();
        des.Mode.Should().Be(PrivacyMode.Partial);
    }
    
    [Fact]
    public void HasField_EmptyFields_ShouldReturnFalse()
    {
        var des = PrivacyModeDescriptor.Create("partial");

        des.HasField("field").Should().BeFalse();
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void HasField_EmptyFieldName_ShouldReturnFalse(string field)
    {
        var des = PrivacyModeDescriptor.Create("partial:field");

        des.HasField(field).Should().BeFalse();
    }
    
    [Theory]
    [InlineData("field")]
    [InlineData("Field")]
    public void HasField_ShouldReturnTrue(string field)
    {
        var des = PrivacyModeDescriptor.Create("partial:field");

        des.HasField(field).Should().BeTrue();
    }
}
