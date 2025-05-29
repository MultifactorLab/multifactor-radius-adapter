using Multifactor.Core.Ldap.Name;
using Multifactor.Radius.Adapter.v2.Core.Ldap.Forest;
using Multifactor.Radius.Adapter.v2.Services.LdapForest;

namespace Multifactor.Radius.Adapter.v2.Tests.LdapForest;

public class ForestMetadataCacheTests
{
    [Fact]
    public void ForestCache_SingleValue_ShouldAdd()
    {
        var cache = new ForestMetadataCache();
        var metadataKey = "metadataKey";
        var root = new DistinguishedName("dc=root");
        
        var values = new Dictionary<string, DistinguishedName>();
        var dict = new KeyValuePair<string, DistinguishedName>("Key", new DistinguishedName("dc=some,dc=domain"));
        values.Add(dict.Key, dict.Value);
        
        var schema = new ForestSchema(root ,values);
        cache.Add(metadataKey, schema);
        var cacheValue = cache.Get(metadataKey, root);
        Assert.NotNull(cacheValue);
    }
    
    [Fact]
    public void ForestCache_SameKey_ShouldAdd()
    {
        var cache = new ForestMetadataCache();
        var metadataKey = "metadataKey";
        var root = new DistinguishedName("dc=root");
        
        var values = new Dictionary<string, DistinguishedName>();
        var dict = new KeyValuePair<string, DistinguishedName>("Key", new DistinguishedName("dc=some,dc=domain"));
        values.Add(dict.Key, dict.Value);
        
        var schema = new ForestSchema(root ,values);
        cache.Add(metadataKey, schema);
        cache.Add(metadataKey, schema);
        var cacheValue = cache.Get(metadataKey, root);
        Assert.NotNull(cacheValue);
    }
    
    [Fact]
    public void ForestCache_DifferentKeys_ShouldAdd()
    {
        var cache = new ForestMetadataCache();
        var metadataKey1 = "metadataKey1";
        var metadataKey2 = "metadataKey2";
        var root = new DistinguishedName("dc=root");
        
        var values = new Dictionary<string, DistinguishedName>();
        var dict = new KeyValuePair<string, DistinguishedName>("Key", new DistinguishedName("dc=some,dc=domain"));
        values.Add(dict.Key, dict.Value);
        
        var schema = new ForestSchema(root ,values);
        cache.Add(metadataKey1, schema);
        cache.Add(metadataKey2, schema);
        var cacheValue = cache.Get(metadataKey1, root);
        Assert.NotNull(cacheValue);
        
        cacheValue = cache.Get(metadataKey2, root);
        Assert.NotNull(cacheValue);
    }
}