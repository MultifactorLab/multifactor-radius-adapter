using Multifactor.Radius.Adapter.v2.Application.Features.FirstFactor.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.FirstFactor.Ports;

public interface ICheckConnection
{
    bool Execute(CheckConnectionDto dto);
}