using Microsoft.Extensions.Caching.Memory;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadSchema.Ports;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.UseCases.LoadSchema;

internal sealed class SchemaCache : ISchemaCache
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);
    private const string CacheKeyPrefix = "Schema";
    private readonly IMemoryCache _memoryCache;
    public SchemaCache(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }
    public void Set(string key, ILdapSchema value)
    {
        var cacheKey = $"{CacheKeyPrefix}:{key}";
        _memoryCache.Set(cacheKey, value, DateTimeOffset.Now.Add(CacheDuration));
    }

    public bool TryGetValue(string key, out ILdapSchema? value)
    {        
        var cacheKey = $"{CacheKeyPrefix}:{key}";
        var result = _memoryCache.TryGetValue(cacheKey, out value);
        return result;
    }
}