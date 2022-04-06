//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using LdapForNet;
using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Core.Services.Ldap;
using MultiFactor.Radius.Adapter.Server;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static LdapForNet.Native.Native;

namespace MultiFactor.Radius.Adapter.Services.Ldap
{
    /// <summary>
    /// Service to interact with LDAP server
    /// </summary>
    public class LdapService
    {
        protected ServiceConfiguration _serviceConfiguration;
        protected ILogger _logger;
        protected virtual LdapNames Names => new LdapNames(LdapServerType.Generic);

        public LdapService(ServiceConfiguration serviceConfiguration, ILogger logger)
        {
            _serviceConfiguration = serviceConfiguration ?? throw new ArgumentNullException(nameof(serviceConfiguration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Verify User Name, Password, User Status and Policy against Active Directory
        /// </summary>
        public async Task<bool> VerifyCredential(string userName, string password, string ldapUri, PendingRequest request, ClientConfiguration clientConfig)
        {
            if (string.IsNullOrEmpty(userName))
            {
                throw new ArgumentNullException(nameof(userName));
            }
            if (string.IsNullOrEmpty(password))
            {
                _logger.Error("Empty password provided for user '{user:l}'", userName);
                return false;
            }
            if (string.IsNullOrEmpty(ldapUri))
            {
                throw new ArgumentNullException(nameof(ldapUri));
            }

            var user = LdapIdentity.ParseUser(userName);
            var bindDn = FormatBindDn(ldapUri, user, clientConfig);

            _logger.Debug($"Verifying user '{{user:l}}' credential and status at {ldapUri}", bindDn);

            try
            {
                using (var connection = new LdapConnection())
                {
                    //trust self-signed certificates on ldap server
                    connection.TrustAllCertificates();

                    if (Uri.IsWellFormedUriString(ldapUri, UriKind.Absolute))
                    {
                        var uri = new Uri(ldapUri);
                        connection.Connect(uri.GetLeftPart(UriPartial.Authority));
                    }
                    else
                    {
                        connection.Connect(ldapUri, 389);
                    }
                    //do not follow chase referrals
                    connection.SetOption(LdapOption.LDAP_OPT_REFERRALS, IntPtr.Zero);

                    await connection.BindAsync(LdapAuthType.Simple, new LdapCredential 
                    {
                        UserName = bindDn,
                        Password = password
                    });

                    var domain = await WhereAmI(ldapUri, connection, clientConfig);

                    _logger.Information($"User '{{user:l}}' credential and status verified successfully in {domain.Name}", user.Name);

                    var profile = await LoadProfile(connection, domain, user, clientConfig);
                    if (profile == null)
                    {
                        return false;
                    }

                    var checkGroupMembership = !string.IsNullOrEmpty(clientConfig.ActiveDirectoryGroup);
                    //user must be member of security group
                    if (checkGroupMembership)
                    {
                        var isMemberOf = await IsMemberOf(connection, domain, user, profile, clientConfig.ActiveDirectoryGroup);

                        if (!isMemberOf)
                        {
                            _logger.Warning($"User '{{user:l}}' is not member of '{clientConfig.ActiveDirectoryGroup}' group in {profile.BaseDn.Name}", user.Name);
                            return false;
                        }

                        _logger.Debug($"User '{{user:l}}' is member of '{clientConfig.ActiveDirectoryGroup}' group in {profile.BaseDn.Name}", user.Name);
                    }

                    var onlyMembersOfGroupMustProcess2faAuthentication = !string.IsNullOrEmpty(clientConfig.ActiveDirectory2FaGroup);
                    //only users from group must process 2fa
                    if (onlyMembersOfGroupMustProcess2faAuthentication)
                    {
                        var isMemberOf = await IsMemberOf(connection, domain, user, profile, clientConfig.ActiveDirectory2FaGroup);

                        if (isMemberOf)
                        {
                            _logger.Debug($"User '{{user:l}}' is member of '{clientConfig.ActiveDirectory2FaGroup}' in {profile.BaseDn.Name}", user.Name);
                        }
                        else
                        {
                            _logger.Information($"User '{{user:l}}' is not member of '{clientConfig.ActiveDirectory2FaGroup}' in {profile.BaseDn.Name}", user.Name);
                            request.Bypass2Fa = true;
                        }
                    }

                    if (clientConfig.UseActiveDirectoryUserPhone)
                    {
                        request.UserPhone = profile.Phone;
                    }
                    if (clientConfig.UseActiveDirectoryMobileUserPhone)
                    {
                        request.UserPhone = profile.Mobile;
                    }
                    request.DisplayName = profile.DisplayName;
                    request.EmailAddress = profile.Email;
                    request.LdapAttrs = profile.LdapAttrs;

                    if (profile.MemberOf != null)
                    {
                        request.UserGroups = profile.MemberOf.Select(dn => LdapIdentity.DnToCn(dn)).ToList();
                    }
                }

                return true; //OK
            }
            catch (LdapException lex)
            {
                if (lex.Message != null)
                {
                    var dataReason = ExtractErrorReason(lex.Message);
                    if (dataReason != null)
                    {
                        _logger.Warning($"Verification user '{{user:l}}' at {ldapUri} failed: {dataReason}", user.Name);
                        return false;
                    }
                }

                _logger.Error(lex, $"Verification user '{{user:l}}' at {ldapUri} failed", user.Name);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Verification user '{{user:l}}' at {ldapUri} failed", user.Name);
            }

            return false;
        }

        protected virtual string FormatBindDn(string ldapUri, LdapIdentity user, ClientConfiguration clientConfig)
        {
            if (user.Type == IdentityType.UserPrincipalName) 
            {
                return user.Name;
            }
            
            var bindDn = $"{Names.Uid}={user.Name}";
            if (!string.IsNullOrEmpty(clientConfig.LdapBindDn))
            {
                bindDn += "," + clientConfig.LdapBindDn;
            }

            return bindDn;
        }

        protected async Task<LdapIdentity> WhereAmI(string host, LdapConnection connection, ClientConfiguration clientConfig)
        {
            var queryResult = await Query(connection, "", "(objectclass=*)", LdapSearchScope.LDAP_SCOPE_BASEOBJECT, "defaultNamingContext");
            var result = queryResult.SingleOrDefault();
            if (result == null)
            {
                throw new InvalidOperationException($"Unable to query {host} for current user");
            }

            var defaultNamingContext = result.DirectoryAttributes["defaultNamingContext"].GetValue<string>();

            return new LdapIdentity { Name = defaultNamingContext, Type = IdentityType.DistinguishedName };
        }

        protected virtual async Task<LdapProfile> LoadProfile(LdapConnection connection, LdapIdentity domain, LdapIdentity user, ClientConfiguration clientConfig)
        {
            var profile = new LdapProfile();

            var queryAttributes = new List<string> { "DistinguishedName", "displayName", "mail", "telephoneNumber", "mobile", "memberOf" };

            var ldapReplyAttributes = clientConfig.GetLdapReplyAttributes();
            foreach(var ldapReplyAttribute in ldapReplyAttributes)
            {
                if (!profile.LdapAttrs.ContainsKey(ldapReplyAttribute))
                {
                    profile.LdapAttrs.Add(ldapReplyAttribute, null);
                    queryAttributes.Add(ldapReplyAttribute);
                }
            }

            var searchFilter = $"(&(objectClass={Names.UserClass})({Names.Identity(user)}={user.Name}))";

            _logger.Debug($"Querying user '{{user:l}}' in {domain.Name}", user.Name);

            var response = await Query(connection, domain.Name, searchFilter, LdapSearchScope.LDAP_SCOPE_SUB, queryAttributes.ToArray());

            var entry = response.SingleOrDefault();
            if (entry == null)
            {
                _logger.Error($"Unable to find user '{{user:l}}' in {domain.Name}", user.Name);
                return null;
            }

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
            if (attrs.TryGetValue("telephoneNumber", out var phoneAttr))
            {
                profile.Phone = phoneAttr.GetValue<string>();
            }
            if (attrs.TryGetValue("mobile", out var mobileAttr))
            {
                profile.Mobile = mobileAttr.GetValue<string>();
            }
            if (attrs.TryGetValue("memberOf", out var memberOfAttr))
            {
                profile.MemberOf = memberOfAttr.GetValues<string>().ToList();
            }

            foreach(var key in profile.LdapAttrs.Keys.ToList()) //to list to avoid collection was modified exception
            {
                if (attrs.TryGetValue(key, out var attrValue))
                {
                    profile.LdapAttrs[key] = attrValue.GetValue<string>();
                }
                else
                {
                    _logger.Warning($"Can't load attribute '{key}' from user '{entry.Dn}'");
                }
            }

            _logger.Debug($"User '{{user:l}}' profile loaded: {profile.DistinguishedName}", user.Name);

            if (clientConfig.ShouldLoadUserGroups())
            {
                await LoadAllUserGroups(connection, domain, profile, clientConfig);
            }

            return profile;
        }

        protected virtual async Task<bool> IsMemberOf(LdapConnection connection, LdapIdentity domain, LdapIdentity user, LdapProfile profile, string groupName)
        {
            var group = await FindValidGroup(connection, domain, groupName);

            if (group == null)
            {
                _logger.Warning($"Group '{groupName}' not exists in {domain.Name}");
                return false;
            }

            return profile.MemberOf?.Any(g => g == group.Name) ?? false;
        }

        protected async Task<LdapIdentity> FindValidGroup(LdapConnection connection, LdapIdentity domain, string groupName)
        {
            var group = LdapIdentity.ParseGroup(groupName);
            var searchFilter = $"(&({Names.ObjectClass}={Names.GroupClass})({Names.Identity(group)}={group.Name}))";
            var response = await Query(connection, domain.Name, searchFilter, LdapSearchScope.LDAP_SCOPE_SUB, "DistinguishedName");

            foreach(var entry in response)
            {
                var baseDn = LdapIdentity.BaseDn(entry.Dn);
                if (baseDn.Name == domain.Name) //only from user domain
                {
                    var validatedGroup = new LdapIdentity
                    {
                        Name = entry.Dn,
                        Type = IdentityType.DistinguishedName
                    };

                    return validatedGroup;
                }
            }

            return null;
        }

        protected async Task<IList<LdapEntry>> Query(LdapConnection connection, string baseDn, string filter, LdapSearchScope scope, params string[] attributes)
        {
            var results = await connection.SearchAsync(baseDn, filter, attributes, scope);
            return results;
        }

        protected virtual Task LoadAllUserGroups(LdapConnection connection, LdapIdentity domain, LdapProfile profile, ClientConfiguration clientConfig)
        {
            //already loaded from memberOf
            return Task.CompletedTask;
        }

        protected string ExtractErrorReason(string errorMessage)
        {
            var pattern = @"data ([0-9a-e]{3})";
            var match = Regex.Match(errorMessage, pattern);

            if (match.Success && match.Groups.Count == 2)
            {
                var data = match.Groups[1].Value;

                switch (data)
                {
                    case "525":
                        return "user not found";
                    case "52e":
                        return "invalid credentials";
                    case "530":
                        return "not permitted to logon at this time​";
                    case "531":
                        return "not permitted to logon at this workstation​";
                    case "532":
                        return "password expired";
                    case "533":
                        return "account disabled";
                    case "701":
                        return "account expired";
                    case "773":
                        return "user must change password";
                    case "775":
                        return "user account locked";
                }
            }

            return null;
        }
    }
}