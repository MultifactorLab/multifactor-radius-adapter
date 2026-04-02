using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.UserGroupLoading.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.UserGroupLoading.Ports;

public interface ILoadGroups
{
    IReadOnlyList<string> Execute(LoadUserGroupDto request);
}