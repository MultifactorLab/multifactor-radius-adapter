using Multifactor.Radius.Adapter.v2.Application.Core.Models.Abstractions;
using Multifactor.Radius.Adapter.v2.Application.Features.LoadProfile.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.LoadProfile.Ports;

public interface IProfileSearch
{
    ILdapProfile? Execute(FindUserDto request);
}

