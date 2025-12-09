namespace Multifactor.Radius.Adapter.v2.Infrastructure.Cache;

public interface ICacheService
{
    void Set<T>(string key, T value, DateTimeOffset expirationDate);
    void Set<T>(string key, T value);
    bool TryGetValue<T>(string key, out T? value);
    void Remove(string key);
}