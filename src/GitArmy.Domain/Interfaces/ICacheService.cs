namespace GitArmy.Domain.Interfaces;

public interface ICacheService
{
    T? Get<T>(string key);
    void Set<T>(string key, T value, TimeSpan expiration);
}
