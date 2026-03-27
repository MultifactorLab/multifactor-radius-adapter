using Multifactor.Radius.Adapter.v2.Application.Features.LoadLdapForest.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.LoadLdapForest.Port;

public interface IForestCacheService
{
    public void Set(string key, IForestMetadata value, DateTimeOffset expirationDate);
    public bool TryGetValue(string key, out IForestMetadata? value);
}