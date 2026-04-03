using Multifactor.Radius.Adapter.v2.Application.Core.Models;

namespace Multifactor.Radius.Adapter.v2.Application.SharedPorts;

public interface IChallengeContextCache
{
    void Set(string key, RadiusPipelineContext value);
    bool TryGetValue(string key, out RadiusPipelineContext? value);
    void Remove(string key);
}