using Moq;
using Multifactor.Radius.Adapter.v2.Application.FirstFactor.BindNameFormat;
using Multifactor.Radius.Adapter.v2.Domain.Ldap.Interfaces;

namespace Multifactor.Radius.Adapter.v2.Tests.Unit.LdapBindNameFormatterTests;

public class ActiveDirectoryTests
{
    [Fact]
    public void FormatName_ShouldReturnSameName()
    {
        //Arrange
        var formatter = new ActiveDirectoryFormatter();

        var profileMock = new Mock<ILdapProfile>();
        var name = "userName";
        
        //Act
        var result = formatter.FormatName(name, profileMock.Object);
        
        //Assert
        Assert.Equal(name, result);
    }
}