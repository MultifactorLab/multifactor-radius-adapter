using Moq;
using Multifactor.Core.Ldap.Name;
using Multifactor.Radius.Adapter.v2.Application.Features.FirstFactor.BindNameFormat;
using Multifactor.Radius.Adapter.v2.Application.Ports.Ldap;

namespace Multifactor.Radius.Adapter.v2.Tests.Unit.LdapBindNameFormatterTests;

public class FreeIpaTests
{
    [Fact]
    public void FormatName_Uid_ShouldReturnDn()
    {
        //Arrange
        var formatter = new FreeIpaFormatter();
        var profileMock = new Mock<ILdapProfile>();
        var dn = new DistinguishedName("cn=user,dc=domain,dc=com");
        profileMock.Setup(x => x.Dn).Returns(dn);
        var name = "userName";
        
        //Act
        var result = formatter.FormatName(name, profileMock.Object);
        
        //Assert
        Assert.Equal(dn.StringRepresentation, result);
    }
    
    [Fact]
    public void FormatName_NetBiosName_ShouldReturnDn()
    {
        //Arrange
        var formatter = new FreeIpaFormatter();
        var profileMock = new Mock<ILdapProfile>();
        var dn = new DistinguishedName("cn=user,dc=domain,dc=com");
        profileMock.Setup(x => x.Dn).Returns(dn);
        var name = "domain\\userName";
        
        //Act
        var result = formatter.FormatName(name, profileMock.Object);
        
        //Assert
        Assert.Equal(dn.StringRepresentation, result);
    }
    
    [Fact]
    public void FormatName_Dn_ShouldReturnDn()
    {
        //Arrange
        var formatter = new FreeIpaFormatter();
        var profileMock = new Mock<ILdapProfile>();
        var name = "dc=domain,dc=com";
        
        //Act
        var result = formatter.FormatName(name, profileMock.Object);
        
        //Assert
        Assert.Equal(name, result);
    }
    
    [Fact]
    public void FormatName_Upn_ShouldReturnUpn()
    {
        //Arrange
        var formatter = new FreeIpaFormatter();
        var profileMock = new Mock<ILdapProfile>();
        var name = "user@domain";
        
        //Act
        var result = formatter.FormatName(name, profileMock.Object);
        
        //Assert
        Assert.Equal(name, result);
    }
}