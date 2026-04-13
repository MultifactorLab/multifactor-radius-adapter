using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadLdapForest.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadLdapForest.Port;

public interface IForestCache
{
    public void Set(string key, IForestMetadata value);
    public bool TryGetValue(string key, out IForestMetadata? value);
}