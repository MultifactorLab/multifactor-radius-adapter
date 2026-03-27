using Multifactor.Radius.Adapter.v2.Application.Features.UserGroupLoading.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.UserGroupLoading.Ports;

public interface ILoadGroups
{
    IReadOnlyList<string> Execute(LoadUserGroupDto request);
}