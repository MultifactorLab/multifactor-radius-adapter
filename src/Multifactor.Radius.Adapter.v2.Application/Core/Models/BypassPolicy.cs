using Multifactor.Radius.Adapter.v2.Application.Core.Enum;

namespace Multifactor.Radius.Adapter.v2.Application.Core.Models;

/// <summary>
/// Granular second-factor bypass policy applied when the MULTIFACTOR API is unreachable (bypass-policy client setting).
/// </summary>
public sealed record BypassPolicy(
    BypassPolicyMode Mode,
    IReadOnlyList<IpEntry> IpWhitelist)
{
    public bool UsesGroups => Mode != BypassPolicyMode.IpOnly;

    public static string ToConfigValue(BypassPolicyMode mode) => mode switch
    {
        BypassPolicyMode.Or => "OR",
        BypassPolicyMode.And => "AND",
        BypassPolicyMode.IpOnly => "IP_ONLY",
        BypassPolicyMode.GroupOnly => "GROUP_ONLY",
        _ => mode.ToString()
    };
}
