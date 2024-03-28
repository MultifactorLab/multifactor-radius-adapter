using LdapForNet;
using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Core.Exceptions;
using MultiFactor.Radius.Adapter.Core.Ldap;
using MultiFactor.Radius.Adapter.Core.Services.Ldap;
using MultiFactor.Radius.Adapter.Services.Ldap.Connection;
using MultiFactor.Radius.Adapter.Services.Ldap.UserGroupsReading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static LdapForNet.Native.Native;

namespace MultiFactor.Radius.Adapter.Services.Ldap.ProfileLoading
{
    public class ProfileLoader
    {
        private readonly UserGroupsSource _userGroupsSource;
        private readonly ILogger _logger;

        public ProfileLoader(UserGroupsSource userGroupsSource, ILogger<ProfileLoader> logger)
        {
            _userGroupsSource = userGroupsSource ?? throw new ArgumentNullException(nameof(userGroupsSource));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ILdapProfile> LoadAsync(IClientConfiguration clientConfig, ILdapConnectionAdapter adapter, LdapIdentity user)
        {
            var queryAttributes = new List<string> { "DistinguishedName", "displayName", "mail", "memberOf", "userPrincipalName" };

            // if an attribute is set for the second factor and it is a new attribute
            if (clientConfig.UseIdentityAttribute && !queryAttributes.Contains(clientConfig.TwoFAIdentityAttribute))
            {
                queryAttributes.Add(clientConfig.TwoFAIdentityAttribute);
            }

            foreach (var ldapReplyAttribute in GetLdapReplyAttributes(clientConfig))
            {
                queryAttributes.Add(ldapReplyAttribute);
            }
            queryAttributes.AddRange(clientConfig.PhoneAttributes);
            var names = LdapNamesFactory.CreateLdapNames(clientConfig.FirstFactorAuthenticationSource);
            var searchFilter = $"(&(objectClass={names.UserClass})({names.Identity(user)}={user.Name}))";

            var domain = await adapter.WhereAmIAsync();
            _logger.LogDebug($"Querying user '{{user:l}}' in {domain.Name}", user.Name);

            var response = await adapter.SearchQueryAsync(domain.Name, searchFilter, LdapSearchScope.LDAP_SCOPE_SUB, queryAttributes.Distinct().ToArray());
            var entry = response.SingleOrDefault();
            if (entry == null) throw new LdapUserNotFoundException(user.Name, domain.Name);         

            var profile = new LdapProfile(LdapIdentity.BaseDn(entry.Dn), entry.Dn);

            var attrs = entry.DirectoryAttributes;
            if (attrs.TryGetValue("displayName", out var displayNameAttr))
            {
                profile.SetDisplayName(displayNameAttr.GetValue<string>());
            }
            if (attrs.TryGetValue("mail", out var mailAttr))
            {
                profile.SetEmail(mailAttr.GetValue<string>());
            }
            if (attrs.TryGetValue("userPrincipalName", out var upnAttr))
            {
                profile.SetUpn(upnAttr.GetValue<string>());
            }
            if (clientConfig.UseIdentityAttribute && attrs.TryGetValue(clientConfig.TwoFAIdentityAttribute, out var identityAttribute))
            {
                profile.SetIdentityAttribute(identityAttribute.GetValue<string>());
            }

            // additional attributes for radius response
            foreach (var attr in GetLdapReplyAttributes(clientConfig))
            {
                if (attrs.TryGetValue(attr, out var attrValue))
                {
                    profile.AddLdapAttr(attr, attrValue.GetValue<string>());
                }
            }

            //groups
            if (attrs.TryGetValue("memberOf", out var memberOfAttr))
            {
                foreach (var group in memberOfAttr.GetValues<string>().Select(dn => LdapIdentity.DnToCn(dn)).ToList())
                {
                    profile.AddMemberOf(group);
                }
            }

            //phone
            foreach (var phoneAttr in clientConfig.PhoneAttributes)
            {
                if (attrs.TryGetValue(phoneAttr, out var phoneValue))
                {
                    var phone = phoneValue.GetValue<string>();
                    if (!string.IsNullOrEmpty(phone))
                    {
                        profile.SetPhone(phone);
                        break;
                    }
                }
            }

            _logger.LogDebug($"User '{{user:l}}' profile loaded: {entry.Dn}", user.Name);

            if (clientConfig.ShouldLoadUserGroups())
            {
                var groups = await _userGroupsSource.GetUserGroupsAsync(clientConfig, adapter, entry.Dn);
                foreach (var group in groups)
                {
                    profile.AddMemberOf(group);
                }
            }

            return profile;
        }

        public async Task<Dictionary<string, string[]>> LoadAttributesAsync(IClientConfiguration clientConfig, ILdapConnectionAdapter adapter, LdapIdentity user, params string[] attrs)
        {

            var names = LdapNamesFactory.CreateLdapNames(clientConfig.FirstFactorAuthenticationSource);
            var searchFilter = $"(&(objectClass={names.UserClass})({names.Identity(user)}={user.Name}))";

            var domain = await adapter.WhereAmIAsync();
            _logger.LogDebug($"Querying user '{{user:l}}' in {domain.Name}", user.Name);

            var response = await adapter.SearchQueryAsync(domain.Name, searchFilter, LdapSearchScope.LDAP_SCOPE_SUB, attrs.Distinct().ToArray());
            var entry = response.SingleOrDefault();
            if (entry == null) throw new LdapUserNotFoundException(user.Name, domain.Name);

            var dirAttrs = entry.DirectoryAttributes;
            var attributes = new Dictionary<string, string[]>();
            foreach (var a in attrs)
            {
                if (dirAttrs.TryGetValue(a, out var reqAttr))
                {
                    attributes[a] = reqAttr.GetValues<string>().ToArray();
                }
            }

            return attributes;
        }

        private static string[] GetLdapReplyAttributes(IClientConfiguration config)
        {
            return config.RadiusReplyAttributes
                .Values
                .SelectMany(attr => attr)
                .Where(attr => attr.FromLdap)
                .Select(attr => attr.LdapAttributeName)
                .ToArray();
        }
    }
}
