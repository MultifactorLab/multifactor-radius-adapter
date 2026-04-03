using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SharedServices.ChallengeProcessor.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SharedServices.ChallengeProcessor.Ports;

public interface IPasswordChangeCache
{
    void Set(string key, PasswordChangeValue value, DateTimeOffset expirationDate);
    bool TryGetValue(string key, out PasswordChangeValue? value);
    void Remove(string key);
}