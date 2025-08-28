using Moq;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Core.FirstFactor.BindNameFormat;

namespace Multifactor.Radius.Adapter.v2.Tests.Unit.LdapBindNameFormatterTests;

public class LdapBindNameFormatterProviderTests
{
    [Theory]
    [InlineData(LdapImplementation.ActiveDirectory)]
    [InlineData(LdapImplementation.OpenLDAP)]
    [InlineData(LdapImplementation.Samba)]
    [InlineData(LdapImplementation.FreeIPA)]
    [InlineData(LdapImplementation.MultiDirectory)]
    public void GetLdapBindNameFormatter_ShouldReturnRequiredFormatter(LdapImplementation ldapImplementation)
    {
        //Arrange
        var processor = new Mock<ILdapBindNameFormatter>();
        processor.Setup(x => x.LdapImplementation).Returns(ldapImplementation);
        var provider = new LdapBindNameFormatterProvider([processor.Object]);
        
        //Act
        var formatter = provider.GetLdapBindNameFormatter(ldapImplementation);
        
        //Assert
        Assert.NotNull(formatter);
        Assert.Equal(ldapImplementation, formatter.LdapImplementation);
    }
    
    [Theory]
    [InlineData(LdapImplementation.ActiveDirectory)]
    [InlineData(LdapImplementation.OpenLDAP)]
    [InlineData(LdapImplementation.Samba)]
    [InlineData(LdapImplementation.FreeIPA)]
    [InlineData(LdapImplementation.MultiDirectory)]
    public void GetLdapBindNameFormatter_NoSuchFormatter_ShouldReturnNull(LdapImplementation ldapImplementation)
    {
        //Arrange
        var processor = new Mock<ILdapBindNameFormatter>();
        processor.Setup(x => x.LdapImplementation).Returns(LdapImplementation.Unknown);
        var provider = new LdapBindNameFormatterProvider([processor.Object]);
        
        //Act
        var formatter = provider.GetLdapBindNameFormatter(ldapImplementation);
        
        //Assert
        Assert.Null(formatter);
    }
}