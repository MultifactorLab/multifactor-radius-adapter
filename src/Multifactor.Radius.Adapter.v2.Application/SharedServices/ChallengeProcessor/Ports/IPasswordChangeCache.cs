using Multifactor.Radius.Adapter.v2.Application.SharedServices.ChallengeProcessor.Models;

namespace Multifactor.Radius.Adapter.v2.Application.SharedServices.ChallengeProcessor.Ports;

public interface IPasswordChangeCache//todo realize
{
    void Set(string key, PasswordChangeCache value, DateTimeOffset expirationDate);
    bool TryGetValue(string key, out PasswordChangeCache? value);
    void Remove(string key);
}