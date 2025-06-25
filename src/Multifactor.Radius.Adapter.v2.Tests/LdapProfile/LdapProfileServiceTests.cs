using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Multifactor.Core.Ldap.Name;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.Ldap;
using Multifactor.Radius.Adapter.v2.Core.Ldap.Identity;
using Multifactor.Radius.Adapter.v2.Services.Ldap;
using Multifactor.Radius.Adapter.v2.Services.LdapForest;
using Multifactor.Radius.Adapter.v2.Services.NetBios;
using Multifactor.Radius.Adapter.v2.Tests.Fixture;

namespace Multifactor.Radius.Adapter.v2.Tests.LdapProfile;

public class LdapProfileServiceTests
{
    [Fact]
    public void LoadProfile_ByDn_ShouldLoadProfile()
    {
        var sensitiveData = GetConfig();
        var searchBase = new DistinguishedName(sensitiveData["SearchBase"]);
        var targetUser = new UserIdentity(sensitiveData["TargetUserDn"]);
        var serverConfig = GetServerConfig(sensitiveData);
        var netBiosServiceMock = new Mock<INetBiosService>();
        var schema =  LdapSchemaBuilder.Create();
        schema.LdapServerImplementation = LdapImplementation.ActiveDirectory;
        var service = new LdapProfileService(new CustomLdapConnectionFactory(), netBiosServiceMock.Object, NullLogger<LdapProfileService>.Instance);
        var ldapProfile = service.FindUserProfile(new FindUserProfileRequest("clientKey", serverConfig, schema, searchBase, targetUser));
        
        Assert.NotNull(ldapProfile);
        
        var expectedUserDn = sensitiveData["TargetUserDn"].ToLower();
        var actualDn = ldapProfile.Dn.StringRepresentation.ToLower();
        Assert.Equal(expectedUserDn, actualDn);
    }
    
    [Fact]
    public void LoadProfile_ByUpn_ShouldLoadProfile()
    {
        var sensitiveData = GetConfig();
        var searchBase = new DistinguishedName(sensitiveData["SearchBase"]);
        var targetUser = new UserIdentity(sensitiveData["TargetUserUpn"]);
        var cacheMock = new Mock<IForestMetadataCache>();
        var netBiosServiceMock = new Mock<INetBiosService>();
        cacheMock.Setup(x => x.Get(It.IsAny<string>(), It.IsAny<DistinguishedName>())).Returns(() => null);
        var serverConfig = GetServerConfig(sensitiveData);
        var schema =  LdapSchemaBuilder.Create();
        schema.LdapServerImplementation = LdapImplementation.ActiveDirectory;
        var service = new LdapProfileService(new CustomLdapConnectionFactory(), netBiosServiceMock.Object, NullLogger<LdapProfileService>.Instance);
        var ldapProfile = service.FindUserProfile(new FindUserProfileRequest("clientKey", serverConfig, schema, searchBase, targetUser));
        
        Assert.NotNull(ldapProfile);
        
        var expectedUserDn = sensitiveData["TargetUserDn"].ToLower();
        var actualDn = ldapProfile.Dn.StringRepresentation.ToLower();
        Assert.Equal(expectedUserDn, actualDn);
    }
    
    [Fact]
    public void LoadProfile_ByUid_ShouldLoadProfile()
    {
        var sensitiveData = GetConfig();
        var searchBase = new DistinguishedName(sensitiveData["SearchBase"]);
        var targetUser = new UserIdentity(sensitiveData["TargetUserUid"]);
        var netBiosServiceMock = new Mock<INetBiosService>();
        var serverConfig = GetServerConfig(sensitiveData);
        var schema =  LdapSchemaBuilder.Create();
        schema.LdapServerImplementation = LdapImplementation.ActiveDirectory;
        var service = new LdapProfileService(new CustomLdapConnectionFactory(), netBiosServiceMock.Object, NullLogger<LdapProfileService>.Instance);
        var ldapProfile = service.FindUserProfile(new FindUserProfileRequest("clientKey", serverConfig, schema, searchBase, targetUser));
        
        Assert.NotNull(ldapProfile);
        
        var expectedUserDn = sensitiveData["TargetUserDn"].ToLower();
        var actualDn = ldapProfile.Dn.StringRepresentation.ToLower();
        Assert.Equal(expectedUserDn, actualDn);
    }
    
    [Fact]
    public void LoadProfile_ByNetBios_ShouldLoadProfile()
    {
        var sensitiveData = GetConfig();
        var searchBase = new DistinguishedName(sensitiveData["SearchBase"]);
        var targetUser = new UserIdentity(sensitiveData["TargetUserNetBios"]);
        var cacheMock = new Mock<IForestMetadataCache>();
        cacheMock.Setup(x => x.Get(It.IsAny<string>(), It.IsAny<DistinguishedName>())).Returns(() => null);
        var netBiosService = new NetBiosService(cacheMock.Object, NullLogger<NetBiosService>.Instance);
        var serverConfig = GetServerConfig(sensitiveData);
        var schema =  LdapSchemaBuilder.Create();
        schema.LdapServerImplementation = LdapImplementation.ActiveDirectory;
        var service = new LdapProfileService(new CustomLdapConnectionFactory(), netBiosService, NullLogger<LdapProfileService>.Instance);
        var ldapProfile = service.FindUserProfile(new FindUserProfileRequest("clientKey", serverConfig, schema, searchBase, targetUser));
        
        Assert.NotNull(ldapProfile);
        
        var expectedUserDn = sensitiveData["TargetUserDn"].ToLower();
        var actualDn = ldapProfile.Dn.StringRepresentation.ToLower();
        Assert.Equal(expectedUserDn, actualDn);
    }

    private ILdapServerConfiguration GetServerConfig(Dictionary<string, string> sensitiveData)
    {
        var serverConfigMock = new Mock<ILdapServerConfiguration>();
        serverConfigMock.Setup(x => x.ConnectionString).Returns(sensitiveData["ConnectionString"]);
        serverConfigMock.Setup(x => x.UserName).Returns(sensitiveData["Admin"]);
        serverConfigMock.Setup(x => x.Password).Returns(sensitiveData["AdminPwd"]);
        serverConfigMock.Setup(x => x.Password).Returns(sensitiveData["AdminPwd"]);
        serverConfigMock.Setup(x => x.BindTimeoutInSeconds).Returns(30);
        return serverConfigMock.Object;
    }

    private Dictionary<string, string> GetConfig()
    {
        return ConfigUtils.GetConfigSensitiveData("LoadProfileService.txt", "|");
    }
}