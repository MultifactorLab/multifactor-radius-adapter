using LdapForNet;
using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Core.Services.Ldap;
using MultiFactor.Radius.Adapter.Services.Ldap;
using MultiFactor.Radius.Adapter.Services.Ldap.Connection;
using MultiFactor.Radius.Adapter.Services.Ldap.UserGroupsGetters;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static LdapForNet.Native.Native;

namespace MultiFactor.Radius.Adapter.Services
{
    public class ProfileLoader
    {
        private readonly UserGroupsGetterProvider _userGroupsGetterProvider;
        private readonly ILogger _logger;

        public ProfileLoader(UserGroupsGetterProvider userGroupsGetterProvider, ILogger logger)
        {
            _userGroupsGetterProvider = userGroupsGetterProvider ?? throw new ArgumentNullException(nameof(userGroupsGetterProvider));
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        }

        public async Task<LdapProfile> LoadAsync(ClientConfiguration clientConfig, 
            LdapConnectionAdapter connAdapter, 
            LdapDomain domain, LdapIdentity user)
        {
            var profile = new LdapProfile();

            var queryAttributes = new List<string> { "DistinguishedName", "displayName", "mail", "memberOf", "userPrincipalName" };

            var ldapReplyAttributes = clientConfig.GetLdapReplyAttributes();
            foreach (var ldapReplyAttribute in ldapReplyAttributes)
            {
                if (!profile.LdapAttrs.ContainsKey(ldapReplyAttribute))
                {
                    profile.LdapAttrs.Add(ldapReplyAttribute, null);
                    queryAttributes.Add(ldapReplyAttribute);
                }
            }
            queryAttributes.AddRange(clientConfig.PhoneAttributes);
            var names = GetLdapNames(clientConfig.FirstFactorAuthenticationSource);
            var searchFilter = $"(&(objectClass={names.UserClass})({names.Identity(user)}={user.Name}))";

            _logger.Debug($"Querying user '{{user:l}}' in {domain.Name}", user.Name);

            var response = await connAdapter.SearchQueryAsync(domain.Name, searchFilter, LdapSearchScope.LDAP_SCOPE_SUB, queryAttributes.Distinct().ToArray());

            var entry = response.SingleOrDefault();
            if (entry == null)
            {
                _logger.Error($"Unable to find user '{{user:l}}' in {domain.Name}", user.Name);
                return null;
            }

            //base profile
            profile.BaseDn = LdapIdentity.BaseDn(entry.Dn);
            profile.DistinguishedName = entry.Dn;

            var attrs = entry.DirectoryAttributes;
            if (attrs.TryGetValue("displayName", out var displayNameAttr))
            {
                profile.DisplayName = displayNameAttr.GetValue<string>();
            }
            if (attrs.TryGetValue("mail", out var mailAttr))
            {
                profile.Email = mailAttr.GetValue<string>();
            }
            if (attrs.TryGetValue("userPrincipalName", out var upnAttr))
            {
                profile.Upn = upnAttr.GetValue<string>();
            }

            //additional attributes for radius response
            foreach (var key in profile.LdapAttrs.Keys.ToList()) //to list to avoid collection was modified exception
            {
                if (attrs.TryGetValue(key, out var attrValue))
                {
                    profile.LdapAttrs[key] = attrValue.GetValue<string>();
                }
            }

            //groups
            if (attrs.TryGetValue("memberOf", out var memberOfAttr))
            {
                profile.MemberOf = memberOfAttr.GetValues<string>().Select(dn => LdapIdentity.DnToCn(dn)).ToList();
            }

            //phone
            foreach (var phoneAttr in clientConfig.PhoneAttributes)
            {
                if (attrs.TryGetValue(phoneAttr, out var phoneValue))
                {
                    var phone = phoneValue.GetValue<string>();
                    if (!string.IsNullOrEmpty(phone))
                    {
                        profile.Phone = phone;
                        break;
                    }
                }
            }

            _logger.Debug($"User '{{user:l}}' profile loaded: {profile.DistinguishedName}", user.Name);

            if (clientConfig.ShouldLoadUserGroups())
            {
                var getter = _userGroupsGetterProvider.GetUserGroupsGetter(clientConfig.FirstFactorAuthenticationSource);
                var groups = await getter.GetAllUserGroupsAsync(clientConfig, connAdapter, domain, profile.DistinguishedName);
                if (groups.Any())
                {
                    profile.MemberOf = groups.ToList();
                }
            }

            return profile;
        }

        private static LdapNames GetLdapNames(AuthenticationSource source)
        {
            switch (source)
            {
                case AuthenticationSource.ActiveDirectory:
                case AuthenticationSource.Radius:
                case AuthenticationSource.None:
                    return new LdapNames(LdapServerType.ActiveDirectory);
                case AuthenticationSource.Ldap:
                    return new LdapNames(LdapServerType.Generic);
                default:
                    throw new NotImplementedException(source.ToString());
            }
        }
    }
}
