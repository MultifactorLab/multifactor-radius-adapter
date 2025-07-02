using MultiFactor.Radius.Adapter.Infrastructure.Configuration.XmlAppConfiguration;

namespace MultiFactor.Radius.Adapter.Tests.AdapterConfig;

[Trait("Category", "App config Reading")]
public class RadiusConfigurationFileTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("file")]
    [InlineData("file.conf")]
    public void Create_WrongPath_ShouldThrow(string path)
    {
        Assert.Throws<ArgumentException>(() => new RadiusConfigurationFile(path));
    }
    
    [Theory]
    [InlineData("file.config")]
    [InlineData("dir/file.config")]
    [InlineData("/etc/configs/file.config")]
    [InlineData("C:\\configs\\file.config")]
    public void Create_CorrectPath_ShouldCreateAndStoreValue(string path)
    {
        var file = new RadiusConfigurationFile(path);
        Assert.Equal(path, file.Path);
    }
    
    [Fact]
    public void Cast_ToStringFromNullRadConfFile_ShouldThrow()
    {
        Assert.Throws<InvalidCastException>(() =>
        {
            RadiusConfigurationFile? file = null;
            _ = (string)file;
        });
    }
    
    [Fact]
    public void Cast_ToStringFromCorrectRadConfFile_ShouldNotThrow()
    {
        RadiusConfigurationFile file = new("dir/file.config");
        var s = (string)file;

        Assert.Equal("dir/file.config", s);
    }
}
