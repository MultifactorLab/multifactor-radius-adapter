using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadUserGroup.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadUserGroup.Ports;

public interface ILoadGroups
{
    IReadOnlyList<string> Execute(LoadUserGroupDto request);
}