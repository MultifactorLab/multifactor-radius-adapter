using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Tests
{
    public class PascalCaseFromDashCaseExtensionTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Fail(string source)
        {
            throw new NotImplementedException();
        }
        
        [Theory]
        [InlineData("myName")]
        [InlineData("my-name")]
        public void Success(string source)
        {
            throw new NotImplementedException();
        }
    }
}
