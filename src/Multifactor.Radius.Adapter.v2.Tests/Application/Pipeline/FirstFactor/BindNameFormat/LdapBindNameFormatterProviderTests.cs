using Moq;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.FirstFactor.BindNameFormat;

namespace Multifactor.Radius.Adapter.v2.Tests.Application.Pipeline.FirstFactor.BindNameFormat
{
    public class LdapBindNameFormatterProviderTests
    {
        [Fact]
        public void GetLdapBindNameFormatter_ShouldReturnCorrectFormatter()
        {
            // Arrange
            var adFormatter = new Mock<ILdapBindNameFormatter>();
            adFormatter.Setup(x => x.LdapImplementation).Returns(LdapImplementation.ActiveDirectory);
            
            var openLdapFormatter = new Mock<ILdapBindNameFormatter>();
            openLdapFormatter.Setup(x => x.LdapImplementation).Returns(LdapImplementation.OpenLDAP);
            
            var formatters = new List<ILdapBindNameFormatter>
            {
                adFormatter.Object,
                openLdapFormatter.Object
            };
            
            var provider = new LdapBindNameFormatterProvider(formatters);

            // Act
            var result = provider.GetLdapBindNameFormatter(LdapImplementation.OpenLDAP);

            // Assert
            Assert.Equal(openLdapFormatter.Object, result);
        }

        [Fact]
        public void GetLdapBindNameFormatter_ShouldReturnNullWhenNotFound()
        {
            // Arrange
            var formatter = new Mock<ILdapBindNameFormatter>();
            formatter.Setup(x => x.LdapImplementation).Returns(LdapImplementation.ActiveDirectory);
            
            var provider = new LdapBindNameFormatterProvider(new[] { formatter.Object });

            // Act
            var result = provider.GetLdapBindNameFormatter(LdapImplementation.OpenLDAP);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetLdapBindNameFormatter_ShouldHandleEmptyFormatters()
        {
            // Arrange
            var provider = new LdapBindNameFormatterProvider(new List<ILdapBindNameFormatter>());

            // Act
            var result = provider.GetLdapBindNameFormatter(LdapImplementation.ActiveDirectory);

            // Assert
            Assert.Null(result);
        }
    }
}