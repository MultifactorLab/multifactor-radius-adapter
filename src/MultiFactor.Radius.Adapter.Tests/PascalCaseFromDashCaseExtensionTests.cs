using MultiFactor.Radius.Adapter.Core.Extensions;

namespace MultiFactor.Radius.Adapter.Tests
{
    public class PascalCaseFromDashCaseExtensionTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Fail(string source)
        {
            Assert.Throws<ArgumentException>(() => source.ToPascalCase());
        }
        
        [Theory]
        [InlineData("myName", "MyName")]
        [InlineData("my-name", "MyName")]
        public void Success(string source, string result)
        {
            var s = source.ToPascalCase();
            Assert.Equal(result, s);
        }
    }
}
