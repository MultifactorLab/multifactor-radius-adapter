//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using System;
using System.Collections.Generic;
using System.Linq;
using MultiFactor.Radius.Adapter.Core.Framework.Context;

namespace MultiFactor.Radius.Adapter.Infrastructure.Configuration.ClientLevel;

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

    /// <summary>
    /// Is match condition
    /// </summary>
    public bool IsMatch(RadiusContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        //if exist ldap attr value
        if (FromLdap)
        {
            //if list of all groups
            if (IsMemberOf)
            {
                return context.UserGroups?.Count > 0;
            }

            //just attribute
            return context.Profile.Attributes.Has(LdapAttributeName);
        }

        // if matched username condition
        if (_userNameCondition.Count != 0)
        {
            var userName = context.UserName;
            var canonicalUserName = Utils.CanonicalizeUserName(userName);
            Func<string, bool> compareLogic = (string conditionName) =>
            {
                var toMatch = Utils.IsCanicalUserName(conditionName)
                    ? canonicalUserName
                    : userName;
                
                return string.Compare(
                    toMatch,
                    conditionName,
                    StringComparison.InvariantCultureIgnoreCase) == 0;
            };
            return _userNameCondition.Any(compareLogic);
        }

        //if matched user group condition
        if (_userGroupCondition.Count != 0)
        {
            return UserGroupCondition.Intersect(
                context.UserGroups,
                StringComparer.OrdinalIgnoreCase
            ).Any();
        }

        return true; //without conditions
    }

    public object[] GetValues(RadiusContext context)
    {
        if (IsMemberOf)
        {
            return context.UserGroups.Select(x => (object)x).ToArray();
        }

        if (FromLdap)
        {
            return new object[] { context.Profile.Attributes.GetValue(LdapAttributeName) };
        }

        return new [] { Value };
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