using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Multifactor.Core.Ldap.Attributes;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.Radius.Attributes;
using Multifactor.Radius.Adapter.v2.Services.Radius;

namespace Multifactor.Radius.Adapter.v2.Tests.Unit;

public class RadiusReplyAttributeServiceTests
{
    [Fact]
    public void NullRequest_ShouldThrowArgumentNullException()
    {
        var radiusDictionaryMock = new Mock<IRadiusDictionary>();
        var converterMock = new RadiusAttributeTypeConverter(radiusDictionaryMock.Object);
        var service = new RadiusReplyAttributeService(converterMock, NullLogger<RadiusReplyAttributeService>.Instance);
        Assert.Throws<ArgumentNullException>(() => service.GetReplyAttributes(null));
    }

    [Fact]
    public void NoReplyAttributes_ShouldReturnEmptyCollection()
    {
        var radiusDictionaryMock = new Mock<IRadiusDictionary>();
        var converterMock = new RadiusAttributeTypeConverter(radiusDictionaryMock.Object);
        var service = new RadiusReplyAttributeService(converterMock, NullLogger<RadiusReplyAttributeService>.Instance);

        var request = new GetReplyAttributesRequest(
            "userName",
            new HashSet<string>(),
            new Dictionary<string, RadiusReplyAttributeValue[]>(),
            new List<LdapAttribute>());
        
        var result = service.GetReplyAttributes(request);
        Assert.Empty(result);
    }

    [Fact]
    public void GetReplyAttributes_ConstantAttribute_ShouldReturnReplyAttributes()
    {
        var replyAttributes = new Dictionary<string, RadiusReplyAttributeValue[]>();
        replyAttributes.Add("key", [new RadiusReplyAttributeValue("const", "")]);
        var request = new GetReplyAttributesRequest(
            "userName",
            new HashSet<string>(),
            replyAttributes,
            new List<LdapAttribute>());
        
        var radiusDictionaryMock = new Mock<IRadiusDictionary>();
        radiusDictionaryMock.Setup(x => x.GetAttribute("key")).Returns(new DictionaryAttribute("key", 1, "string"));
        var converterMock = new RadiusAttributeTypeConverter(radiusDictionaryMock.Object);
        var service = new RadiusReplyAttributeService(converterMock, NullLogger<RadiusReplyAttributeService>.Instance);
        var result = service.GetReplyAttributes(request);
        Assert.Single(result);
        var replyAttrVal = result.First().Value.FirstOrDefault();
        Assert.NotNull(replyAttrVal);
        Assert.Equal("const", replyAttrVal as string);
    }

    [Fact]
    public void GetReplyAttributes_FromLdapAttribute_ShouldReturnReplyAttributes()
    {
        var replyAttributes = new Dictionary<string, RadiusReplyAttributeValue[]>();
        replyAttributes.Add("key", [new RadiusReplyAttributeValue("const")]);
        
        var ldapAttributes = new List<LdapAttribute>() { new("const", "fromLdap") };
        var request = new GetReplyAttributesRequest(
            "userName",
            new HashSet<string>(),
            replyAttributes,
            ldapAttributes);
        
        var radiusDictionaryMock = new Mock<IRadiusDictionary>();
        radiusDictionaryMock.Setup(x => x.GetAttribute("key")).Returns(new DictionaryAttribute("key", 1, "string"));
        var converterMock = new RadiusAttributeTypeConverter(radiusDictionaryMock.Object);
        var service = new RadiusReplyAttributeService(converterMock, NullLogger<RadiusReplyAttributeService>.Instance);
        var result = service.GetReplyAttributes(request);
        Assert.Single(result);
        var replyAttrVal = result.First().Value.FirstOrDefault();
        Assert.NotNull(replyAttrVal);
        Assert.Equal("fromLdap", replyAttrVal as string);
    }

    [Fact]
    public void GetReplyAttributes_NoLdapAttribute_ShouldReturnEmptyValues()
    {
        var replyAttributes = new Dictionary<string, RadiusReplyAttributeValue[]>();
        replyAttributes.Add("key", [new RadiusReplyAttributeValue("const")]);
        
        var ldapAttributes = new List<LdapAttribute>() { new("const2", "fromLdap") };
        var request = new GetReplyAttributesRequest(
            "userName",
            new HashSet<string>(),
            replyAttributes,
            ldapAttributes);
        
        var radiusDictionaryMock = new Mock<IRadiusDictionary>();
        radiusDictionaryMock.Setup(x => x.GetAttribute("key")).Returns(new DictionaryAttribute("key", 1, "string"));
        var converterMock = new RadiusAttributeTypeConverter(radiusDictionaryMock.Object);
        var service = new RadiusReplyAttributeService(converterMock, NullLogger<RadiusReplyAttributeService>.Instance);
        var result = service.GetReplyAttributes(request);
        var attr = result.First().Value;
        Assert.Empty(attr);
    }

    [Fact]
    public void GetReplyAttributes_MemberOfAttribute_ShouldReturnReplyAttributes()
    {
        var replyAttributes = new Dictionary<string, RadiusReplyAttributeValue[]>();
        replyAttributes.Add("key", [new RadiusReplyAttributeValue("memberof")]);
        var userGroups = new HashSet<string>();
        userGroups.Add("group1");
        var request = new GetReplyAttributesRequest(
            "userName",
            userGroups,
            replyAttributes,
            new List<LdapAttribute>());
        
        var radiusDictionaryMock = new Mock<IRadiusDictionary>();
        radiusDictionaryMock.Setup(x => x.GetAttribute("key")).Returns(new DictionaryAttribute("key", 1, "string"));
        var converterMock = new RadiusAttributeTypeConverter(radiusDictionaryMock.Object);
        var service = new RadiusReplyAttributeService(converterMock, NullLogger<RadiusReplyAttributeService>.Instance);
        var result = service.GetReplyAttributes(request);
        var attr = result.First().Value.FirstOrDefault();
        Assert.Equal("group1", attr as string);
    }
    
    [Fact]
    public void GetReplyAttributes_NoMemberOfAttribute_ShouldReturnEmptyValues()
    {
        var replyAttributes = new Dictionary<string, RadiusReplyAttributeValue[]>();
        replyAttributes.Add("key", [new RadiusReplyAttributeValue("memberof")]);
        var request = new GetReplyAttributesRequest(
            "userName",
            new HashSet<string>(),
            replyAttributes,
            new List<LdapAttribute>());
        
        var radiusDictionaryMock = new Mock<IRadiusDictionary>();
        radiusDictionaryMock.Setup(x => x.GetAttribute("key")).Returns(new DictionaryAttribute("key", 1, "string"));
        var converterMock = new RadiusAttributeTypeConverter(radiusDictionaryMock.Object);
        var service = new RadiusReplyAttributeService(converterMock, NullLogger<RadiusReplyAttributeService>.Instance);
        var result = service.GetReplyAttributes(request);
        var attr = result.First().Value;
        Assert.Empty(attr);
    }

    [Fact]
    public void GetReplyAttributes_UserNameCondition_ShouldReturnReplyAttributes()
    {
        var replyAttributes = new Dictionary<string, RadiusReplyAttributeValue[]>();
        replyAttributes.Add("key", [new RadiusReplyAttributeValue("const", "UserName=userName")]);
        var request = new GetReplyAttributesRequest(
            "userName",
            new HashSet<string>(),
            replyAttributes,
            new List<LdapAttribute>());
        var radiusDictionaryMock = new Mock<IRadiusDictionary>();
        radiusDictionaryMock.Setup(x => x.GetAttribute("key")).Returns(new DictionaryAttribute("key", 1, "string"));
        var converterMock = new RadiusAttributeTypeConverter(radiusDictionaryMock.Object);
        var service = new RadiusReplyAttributeService(converterMock, NullLogger<RadiusReplyAttributeService>.Instance);
        
        var result = service.GetReplyAttributes(request);
        var attr = result.First().Value.FirstOrDefault();
        Assert.Equal("const", attr as string);
    }
    
    [Fact]
    public void GetReplyAttributes_InappropriateUserNameCondition_ShouldReturnEmptyValues()
    {
        var replyAttributes = new Dictionary<string, RadiusReplyAttributeValue[]>();
        replyAttributes.Add("key", [new RadiusReplyAttributeValue("const", "UserName=userName1")]);
        var request = new GetReplyAttributesRequest(
            "userName",
            new HashSet<string>(),
            replyAttributes,
            new List<LdapAttribute>());
        var radiusDictionaryMock = new Mock<IRadiusDictionary>();
        radiusDictionaryMock.Setup(x => x.GetAttribute("key")).Returns(new DictionaryAttribute("key", 1, "string"));
        var converterMock = new RadiusAttributeTypeConverter(radiusDictionaryMock.Object);
        var service = new RadiusReplyAttributeService(converterMock, NullLogger<RadiusReplyAttributeService>.Instance);
        
        var result = service.GetReplyAttributes(request);
        var attr = result.First().Value;
        
        Assert.Empty(attr);
    }
    
    [Fact]
    public void GetReplyAttributes_UserGroupCondition_ShouldReturnReplyAttributes()
    {
        var replyAttributes = new Dictionary<string, RadiusReplyAttributeValue[]>();
        replyAttributes.Add("key", [new RadiusReplyAttributeValue("const", "UserGroup=group1")]);
        var userGroups = new HashSet<string>();
        userGroups.Add("group1");
        var request = new GetReplyAttributesRequest(
            "userName",
            userGroups,
            replyAttributes,
            new List<LdapAttribute>());
        var radiusDictionaryMock = new Mock<IRadiusDictionary>();
        radiusDictionaryMock.Setup(x => x.GetAttribute("key")).Returns(new DictionaryAttribute("key", 1, "string"));
        var converterMock = new RadiusAttributeTypeConverter(radiusDictionaryMock.Object);
        var service = new RadiusReplyAttributeService(converterMock, NullLogger<RadiusReplyAttributeService>.Instance);
        
        var result = service.GetReplyAttributes(request);
        var attr = result.First().Value.FirstOrDefault();
        Assert.Equal("const", attr as string);
    }
    
    [Fact]
    public void GetReplyAttributes_InappropriateUserGroupCondition_ShouldReturnEmptyValues()
    {
        var replyAttributes = new Dictionary<string, RadiusReplyAttributeValue[]>();
        replyAttributes.Add("key", [new RadiusReplyAttributeValue("const", "UserGroup=group2")]);
        var userGroups = new HashSet<string>();
        userGroups.Add("group1");
        var request = new GetReplyAttributesRequest(
            "userName",
            userGroups,
            replyAttributes,
            new List<LdapAttribute>());
        var radiusDictionaryMock = new Mock<IRadiusDictionary>();
        radiusDictionaryMock.Setup(x => x.GetAttribute("key")).Returns(new DictionaryAttribute("key", 1, "string"));
        var converterMock = new RadiusAttributeTypeConverter(radiusDictionaryMock.Object);
        
        var service = new RadiusReplyAttributeService(converterMock, NullLogger<RadiusReplyAttributeService>.Instance);
        
        var result = service.GetReplyAttributes(request);
        var attr = result.First().Value;
        
        Assert.Empty(attr);
    }
}