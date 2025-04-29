//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

namespace Multifactor.Radius.Adapter.v2.Core.Configuration.Client;

/// <summary>
/// Radius Access-Accept message extra element
/// </summary>
public class RadiusReplyAttributeValue
{
    public bool FromLdap { get; }

    /// <summary>
    /// Attribute Value
    /// </summary>
    public object Value { get; }

    public bool Sufficient { get; }

    /// <summary>
    /// Ldap attr name to proxy value from
    /// </summary>
    public string LdapAttributeName { get; }

    /// <summary>
    /// Is list of all user groups attribute
    /// </summary>
    public bool IsMemberOf => LdapAttributeName?.ToLower() == "memberof";

    private readonly List<string> _userGroupCondition = new();
    /// <summary>
    /// User group condition
    /// </summary>
    public string[] UserGroupCondition => _userGroupCondition.ToArray();

    private readonly List<string> _userNameCondition = new();

    /// <summary>
    /// User name condition
    /// </summary>
    public string[] UserNameCondition => _userNameCondition.ToArray();

    /// <summary>
    /// Const value with optional condition
    /// </summary>
    public RadiusReplyAttributeValue(object value, string conditionClause, bool sufficient = false)
    {
        Value = value;
        if (!string.IsNullOrEmpty(conditionClause))
        {
            ParseConditionClause(conditionClause);
        }
        Sufficient = sufficient;
    }

    /// <summary>
    /// Proxy value from LDAP attr
    /// </summary>
    public RadiusReplyAttributeValue(string ldapAttributeName, bool sufficient = false)
    {
        if (string.IsNullOrEmpty(ldapAttributeName))
        {
            throw new ArgumentNullException(nameof(ldapAttributeName));
        }
            
        LdapAttributeName = ldapAttributeName;
        FromLdap = true;
        Sufficient = sufficient;
    }

    private void ParseConditionClause(string clause)
    {
        var parts = clause.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);

        switch (parts[0])
        {
            case "UserGroup":
                _userGroupCondition.AddRange(parts[1].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()));
                break;
            
            case "UserName":
                _userNameCondition.AddRange(parts[1].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()));
                break;
            
            default:
                throw new Exception($"Unknown condition '{clause}'");
        }
    }
}