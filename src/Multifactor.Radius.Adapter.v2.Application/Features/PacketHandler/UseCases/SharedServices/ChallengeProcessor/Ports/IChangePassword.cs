using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SharedServices.ChallengeProcessor.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SharedServices.ChallengeProcessor.Ports;

public interface IChangePassword
{
    bool Execute(ChangeUserPasswordDto request);
}