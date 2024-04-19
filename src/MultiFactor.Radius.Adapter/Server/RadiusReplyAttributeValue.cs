//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using System;
using System.Linq;
using MultiFactor.Radius.Adapter.Framework.Context;

namespace MultiFactor.Radius.Adapter.Server
{
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

        /// <summary>
        /// User group condition
        /// </summary>
        public string UserGroupCondition { get; private set; }

        /// <summary>
        /// User name condition
        /// </summary>
        public string UserNameCondition { get; private set; }

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

            //if matched user name condition
            if (!string.IsNullOrEmpty(UserNameCondition))
            {
                var userName = context.UserName;
                var isCanonical = Utils.IsCanicalUserName(UserNameCondition);
                if (isCanonical)
                {
                    userName = Utils.CanonicalizeUserName(userName);
                }

                return string.Compare(userName, UserNameCondition, StringComparison.InvariantCultureIgnoreCase) == 0;
            }

            //if matched user group condition
            if (!string.IsNullOrEmpty(UserGroupCondition))
            {
                var isInGroup = context
                    .UserGroups
                    .Any(g => string.Compare(g, UserGroupCondition, StringComparison.InvariantCultureIgnoreCase) == 0);

                return isInGroup;
            }

            return true; //without conditions
        }

        public object[] GetValues(RadiusContext context)
        {
            if (IsMemberOf)
            {
                return context.UserGroups.ToArray();
            }

            if (FromLdap)
            {
                return new object[] { context.Profile.Attributes.GetValue(LdapAttributeName) };
            }

            return new object[] { Value };
        }

        private void ParseConditionClause(string clause)
        {
            var parts = clause.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);

            switch (parts[0])
            {
                case "UserGroup":
                    UserGroupCondition = parts[1];
                    break;
                case "UserName":
                    UserNameCondition = parts[1];
                    break;
                default:
                    throw new Exception($"Unknown condition '{clause}'");
            }
        }
    }
}
