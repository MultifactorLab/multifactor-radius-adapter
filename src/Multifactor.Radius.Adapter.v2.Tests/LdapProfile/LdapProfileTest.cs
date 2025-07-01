using Multifactor.Core.Ldap.Attributes;
using Multifactor.Core.Ldap.Entry;
using Multifactor.Core.Ldap.Name;

namespace Multifactor.Radius.Adapter.v2.Tests.LdapProfile;

[Collection("ActiveDirectory")]
public class LdapProfileTest
{
    [Fact]
    public void CreateLdapProfile_EntryIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new Core.Ldap.LdapProfile(null));
    }

    [Fact]
    public void CreateLdapProfile_ShouldCreateLdapProfile()
    {
        var dn = new DistinguishedName("dc=example,dc=com");
        var attributes = new LdapAttribute[]
        {
        };
        var ldapAttrCollection = new LdapAttributeCollection(attributes);
        var entry = new LdapEntry(dn, ldapAttrCollection);

        var profile = new Core.Ldap.LdapProfile(entry);
        Assert.NotNull(profile);
        Assert.Equal(dn, profile.Dn);
    }

    [Fact]
    public void CreateLdapProfile_MemberOfAttribute_ShouldReturnMemberOf()
    {
        var dn = new DistinguishedName("dc=example,dc=com");
        var group1 = "cn=group1,dc=example,dc=com";
        var group2 = "cn=group2,dc=example,dc=com";
        var attributes = new LdapAttribute[]
        {
            new(new LdapAttributeName("memberOf"), [group1, group2]),
        };
        var ldapAttrCollection = new LdapAttributeCollection(attributes);
        var entry = new LdapEntry(dn, ldapAttrCollection);

        var profile = new Core.Ldap.LdapProfile(entry);
        
        var memberOf = profile.MemberOf.OrderBy(x =>x.StringRepresentation);
        Assert.NotNull(memberOf);
        var expected = new[] { new DistinguishedName(group1), new DistinguishedName(group2) }.OrderBy(x =>x.StringRepresentation);
        Assert.True(expected.SequenceEqual(memberOf));
    }
    
    [Fact]
    public void CreateLdapProfile_UpnAttribute_ShouldReturnUpn()
    {
        var dn = new DistinguishedName("dc=example,dc=com");
        var upn = "user@domain.com";
        
        var attributes = new LdapAttribute[]
        {
            new(new LdapAttributeName("userPrincipalName"), [upn]),
        };
        var ldapAttrCollection = new LdapAttributeCollection(attributes);
        var entry = new LdapEntry(dn, ldapAttrCollection);

        var profile = new Core.Ldap.LdapProfile(entry);

        var upnFromProfile = profile.Upn;
        Assert.Equal(upn, upnFromProfile);
    }
    
    [Fact]
    public void CreateLdapProfile_PhoneAttribute_ShouldReturnPhone()
    {
        var dn = new DistinguishedName("dc=example,dc=com");
        var phone = "somephone";
        
        var attributes = new LdapAttribute[]
        {
            new(new LdapAttributeName("mobile"), [phone]),
        };
        var ldapAttrCollection = new LdapAttributeCollection(attributes);
        var entry = new LdapEntry(dn, ldapAttrCollection);

        var profile = new Core.Ldap.LdapProfile(entry);

        var phoneFromProfile = profile.Phone;
        Assert.Equal(phone, phoneFromProfile);
    }
    
    [Fact]
    public void CreateLdapProfile_EmailAttribute_ShouldReturnEmail()
    {
        var dn = new DistinguishedName("dc=example,dc=com");
        var email = "someEmail";
        
        var attributes = new LdapAttribute[]
        {
            new(new LdapAttributeName("email"), [email]),
        };
        var ldapAttrCollection = new LdapAttributeCollection(attributes);
        var entry = new LdapEntry(dn, ldapAttrCollection);

        var profile = new Core.Ldap.LdapProfile(entry);

        var emailFromProfile = profile.Email;
        Assert.NotNull(emailFromProfile);
        Assert.Equal(email, emailFromProfile);
    }
    
    [Fact]
    public void CreateLdapProfile_MailAttribute_ShouldReturnMail()
    {
        var dn = new DistinguishedName("dc=example,dc=com");
        var email = "someEmail";
        
        var attributes = new LdapAttribute[]
        {
            new(new LdapAttributeName("mail"), [email]),
        };
        var ldapAttrCollection = new LdapAttributeCollection(attributes);
        var entry = new LdapEntry(dn, ldapAttrCollection);

        var profile = new Core.Ldap.LdapProfile(entry);

        var emailFromProfile = profile.Email;
        Assert.NotNull(emailFromProfile);
        Assert.Equal(email, emailFromProfile);
    }
    
    [Fact]
    public void CreateLdapProfile_GetAttributes_ShouldReturnAttributes()
    {
        var dn = new DistinguishedName("dc=example,dc=com");
        var email = "someEmail";
        var phone = "somePhone";
        var attr1 = new LdapAttribute(new LdapAttributeName("email"), [email]);
        var attr2 = new LdapAttribute(new LdapAttributeName("phone"), [phone]);
        var attributes = new LdapAttribute[]
        {
            attr1,
            attr2
        };
        var ldapAttrCollection = new LdapAttributeCollection(attributes);
        var entry = new LdapEntry(dn, ldapAttrCollection);

        var profile = new Core.Ldap.LdapProfile(entry);

        var attributesFromProfile = profile.Attributes;
        Assert.Equal(2, attributesFromProfile.Count);
        Assert.Contains(attr1, attributesFromProfile);
        Assert.Contains(attr2, attributesFromProfile);
    }
}