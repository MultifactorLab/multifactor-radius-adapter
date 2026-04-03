namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.Ports;

public interface IPacketKeyCache
{
    void Set(string key);
    bool HasValue(string key);
}