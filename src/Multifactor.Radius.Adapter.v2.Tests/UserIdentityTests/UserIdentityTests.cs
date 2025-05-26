using Multifactor.Radius.Adapter.v2.Core.Ldap.Identity;
using Multifactor.Radius.Adapter.v2.Tests.Fixture;

namespace Multifactor.Radius.Adapter.v2.Tests.UserIdentityTests;

public class UserIdentityTests
{
    [Theory]
    [InlineData("user@domain", UserIdentityFormat.UserPrincipalName)]
    [InlineData("cn=user,dc=domain", UserIdentityFormat.DistinguishedName)]
    [InlineData("domain\\user", UserIdentityFormat.NetBiosName)]
    [InlineData("user", UserIdentityFormat.SamAccountName)]
    public void UserIdentity_ShouldCreateIdentity(string name, UserIdentityFormat userIdentityFormat)
    {
        var identity = new UserIdentity(name, userIdentityFormat);
        Assert.Equal(name, identity.Identity);
        Assert.Equal(userIdentityFormat, identity.Format);
    }
    
    [Theory]
    [InlineData("user@domain", UserIdentityFormat.UserPrincipalName)]
    [InlineData("cn=user,dc=domain", UserIdentityFormat.DistinguishedName)]
    [InlineData("domain\\user", UserIdentityFormat.NetBiosName)]
    [InlineData("user", UserIdentityFormat.SamAccountName)]
    public void UserIdentity_ShouldParseIdentity(string name, UserIdentityFormat userIdentityFormat)
    {
        var identity = new UserIdentity(name);
        Assert.Equal(name, identity.Identity);
        Assert.Equal(userIdentityFormat, identity.Format);
    }

    [Theory]
    [ClassData(typeof(EmptyStringsListInput))]
    public void UserIdentity_EmptyIdentity_ShouldThrowArgumentException(string name)
    {  
       Assert.ThrowsAny<ArgumentException>(() => new UserIdentity(name));
    }
}