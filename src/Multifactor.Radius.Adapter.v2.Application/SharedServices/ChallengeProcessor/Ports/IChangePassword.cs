using Multifactor.Radius.Adapter.v2.Application.SharedServices.ChallengeProcessor.Models;

namespace Multifactor.Radius.Adapter.v2.Application.SharedServices.ChallengeProcessor.Ports;

public interface IChangePassword
{
    bool Execute(ChangeUserPasswordDto request);
}