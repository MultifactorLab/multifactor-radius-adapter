using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.FirstFactor.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.FirstFactor.Ports;

public interface ICheckConnection
{
    bool Execute(CheckConnectionDto dto);
}