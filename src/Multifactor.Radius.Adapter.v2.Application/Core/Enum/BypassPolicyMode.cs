namespace Multifactor.Radius.Adapter.v2.Application.Core.Enum;

/// <summary>
/// How Source IP and LDAP group checks are combined by the [bypass] policy when the MULTIFACTOR API is unreachable.
/// </summary>
public enum BypassPolicyMode
{
    /// <summary>
    /// Source IP is in bypass-ip-whitelist OR the user is a member of bypass-second-factor-when-api-unreachable-groups.
    /// </summary>
    Or,
    /// <summary>
    /// Source IP is in bypass-ip-whitelist AND the user is a member of bypass-second-factor-when-api-unreachable-groups.
    /// </summary>
    And,
    /// <summary>
    /// Only Source IP is checked, LDAP groups are ignored.
    /// </summary>
    IpOnly,
    /// <summary>
    /// Only LDAP group membership is checked, Source IP is ignored.
    /// </summary>
    GroupOnly
}
