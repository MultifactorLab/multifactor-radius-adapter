namespace Multifactor.Radius.Adapter.v2.Application.Cache;

public interface ICacheService
{
    //TODO check how use. 
    void Set<T>(string key, T value, DateTimeOffset expirationDate);
    bool TryGetValue<T>(string key, out T? value);
    void Remove(string key);
}