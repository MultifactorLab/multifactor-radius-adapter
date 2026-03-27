using Multifactor.Radius.Adapter.v2.Application.Core.Models.Dto;

namespace Multifactor.Radius.Adapter.v2.Application.SharedPorts;

public interface ICheckMembership
{
    bool Execute(MembershipDto dto);
}