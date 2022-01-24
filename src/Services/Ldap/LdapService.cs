//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using LdapForNet;
using MultiFactor.Radius.Adapter.Core.Services.Ldap;
using MultiFactor.Radius.Adapter.Server;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static LdapForNet.Native.Native;

namespace MultiFactor.Radius.Adapter.Services.Ldap
{
    /// <summary>
    /// Service to interact with LDAP server
    /// </summary>
    public class LdapService
    {
        protected Configuration _configuration;
        protected ILogger _logger;

        protected virtual LdapNames Names => new LdapNames(LdapServerType.Generic);

        public LdapService(Configuration configuration, ILogger logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Verify User Name, Password, User Status and Policy against Active Directory
        /// </summary>
        public bool VerifyCredential(string userName, string password, PendingRequest request)
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

            var user = LdapIdentity.ParseUser(userName);
            var bindDn = FormatBindDn(user);

            _logger.Debug($"Verifying user '{{user:l}}' credential and status at {_configuration.ActiveDirectoryDomain}", bindDn);

            try
            {
                using (var connection = new LdapConnection())
                {
                    //trust self-signed certificates on ldap server
                    connection.TrustAllCertificates();

                    if (Uri.IsWellFormedUriString(_configuration.ActiveDirectoryDomain, UriKind.Absolute))
                    {
                        var uri = new Uri(_configuration.ActiveDirectoryDomain);
                        connection.Connect(uri.GetLeftPart(UriPartial.Authority));
                    }
                    else
                    {
                        connection.Connect(_configuration.ActiveDirectoryDomain, 389);
                    }
                    //do not follow chase referrals
                    connection.SetOption(LdapOption.LDAP_OPT_REFERRALS, IntPtr.Zero);

                    connection.Bind(LdapAuthType.Simple, new LdapCredential 
                    {
                        UserName = bindDn,
                        Password = password
                    });

                    var domain = WhereAmI(connection);

                    _logger.Information($"User '{{user:l}}' credential and status verified successfully in {domain.Name}", user.Name);

                    var isProfileLoaded = LoadProfile(connection, domain, user, out var profile);
                    if (!isProfileLoaded)
                    {
                        return false;
                    }

                    var checkGroupMembership = !string.IsNullOrEmpty(_configuration.ActiveDirectoryGroup);
                    //user must be member of security group
                    if (checkGroupMembership)
                    {
                        var isMemberOf = IsMemberOf(connection, domain, user, profile, _configuration.ActiveDirectoryGroup);

                        if (!isMemberOf)
                        {
                            _logger.Warning($"User '{{user:l}}' is not member of '{_configuration.ActiveDirectoryGroup}' group in {profile.BaseDn.Name}", user.Name);
                            return false;
                        }

                        _logger.Debug($"User '{{user:l}}' is member of '{_configuration.ActiveDirectoryGroup}' group in {profile.BaseDn.Name}", user.Name);
                    }

                    var onlyMembersOfGroupMustProcess2faAuthentication = !string.IsNullOrEmpty(_configuration.ActiveDirectory2FaGroup);
                    //only users from group must process 2fa
                    if (onlyMembersOfGroupMustProcess2faAuthentication)
                    {
                        var isMemberOf = IsMemberOf(connection, domain, user, profile, _configuration.ActiveDirectory2FaGroup);

                        if (isMemberOf)
                        {
                            _logger.Debug($"User '{{user:l}}' is member of '{_configuration.ActiveDirectory2FaGroup}' in {profile.BaseDn.Name}", user.Name);
                        }
                        else
                        {
                            _logger.Information($"User '{{user:l}}' is not member of '{_configuration.ActiveDirectory2FaGroup}' in {profile.BaseDn.Name}", user.Name);
                            request.Bypass2Fa = true;
                        }
                    }

                    //check groups membership for radius reply conditional attributes
                    foreach (var attribute in _configuration.RadiusReplyAttributes)
                    {
                        foreach (var value in attribute.Value.Where(val => val.UserGroupCondition != null))
                        {
                            if (IsMemberOf(connection, domain, user, profile, value.UserGroupCondition))
                            {
                                request.UserGroups.Add(value.UserGroupCondition);
                            }
                        }
                    }

                    if (_configuration.UseActiveDirectoryUserPhone)
                    {
                        request.UserPhone = profile.Phone;
                    }
                    if (_configuration.UseActiveDirectoryMobileUserPhone)
                    {
                        request.UserPhone = profile.Mobile;
                    }
                    request.DisplayName = profile.DisplayName;
                    request.EmailAddress = profile.Email;
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
                        _logger.Warning($"Verification user '{{user:l}}' at {_configuration.ActiveDirectoryDomain} failed: {dataReason}", user.Name);
                        return false;
                    }
                }

                _logger.Error(lex, $"Verification user '{{user:l}}' at {_configuration.ActiveDirectoryDomain} failed", user.Name);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Verification user '{{user:l}}' at {_configuration.ActiveDirectoryDomain} failed", user.Name);
            }

            return false;
        }

        protected virtual string FormatBindDn(LdapIdentity user)
        {
            if (user.Type == IdentityType.UserPrincipalName) 
            {
                return user.Name;
            }
            
            var bindDn = $"{Names.Uid}={user.Name}";
            if (!string.IsNullOrEmpty(_configuration.LdapBindDn))
            {
                bindDn += "," + _configuration.LdapBindDn;
            }

            return bindDn;
        }

        protected LdapIdentity WhereAmI(LdapConnection connection)
        {
            var result = Query(connection, null, "(objectclass=*)", LdapSearchScope.LDAP_SCOPE_BASEOBJECT, "defaultNamingContext").SingleOrDefault();
            if (result == null)
            {
                throw new InvalidOperationException($"Unable to query {_configuration.ActiveDirectoryDomain} for current user");
            }

            var defaultNamingContext = result.DirectoryAttributes["defaultNamingContext"].GetValue<string>();

            return new LdapIdentity { Name = defaultNamingContext, Type = IdentityType.DistinguishedName };
        }

        protected virtual bool LoadProfile(LdapConnection connection, LdapIdentity domain, LdapIdentity user, out LdapProfile profile)
        {
            profile = null;

            var attributes = new[] { "DistinguishedName", "displayName", "mail", "telephoneNumber", "mobile", "memberOf" };
            var searchFilter = $"(&(objectClass={Names.UserClass})({Names.Identity(user)}={user.Name}))";

            _logger.Debug($"Querying user '{{user:l}}' in {domain.Name}", user.Name);

            var response = Query(connection, domain.Name, searchFilter, LdapSearchScope.LDAP_SCOPE_SUB, attributes);

            var entry = response.SingleOrDefault();
            if (entry == null)
            {
                _logger.Error($"Unable to find user '{{user:l}}' in {domain.Name}", user.Name);
                return false;
            }

            profile = new LdapProfile
            {
                BaseDn = LdapIdentity.BaseDn(entry.Dn),
                DistinguishedName = entry.Dn,
            };

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

            _logger.Debug($"User '{{user:l}}' profile loaded: {profile.DistinguishedName}", user.Name);

            return true;
        }

        protected virtual bool IsMemberOf(LdapConnection connection, LdapIdentity domain, LdapIdentity user, LdapProfile profile, string groupName)
        {
            var isValidGroup = IsValidGroup(connection, domain, groupName, out var group);

            if (!isValidGroup)
            {
                _logger.Warning($"Group '{groupName}' not exists in {domain.Name}");
                return false;
            }

            return profile.MemberOf?.Any(g => g == group.Name) ?? false;
        }

        protected bool IsValidGroup(LdapConnection connection, LdapIdentity domain, string groupName, out LdapIdentity validatedGroup)
        {
            validatedGroup = null;

            var group = LdapIdentity.ParseGroup(groupName);
            var searchFilter = $"(&({Names.ObjectClass}={Names.GroupClass})({Names.Identity(group)}={group.Name}))";
            var response = Query(connection, domain.Name, searchFilter, LdapSearchScope.LDAP_SCOPE_SUB, "DistinguishedName");

            foreach(var entry in response)
            {
                var baseDn = LdapIdentity.BaseDn(entry.Dn);
                if (baseDn.Name == domain.Name) //only from user domain
                {
                    validatedGroup = new LdapIdentity
                    {
                        Name = entry.Dn,
                        Type = IdentityType.DistinguishedName
                    };

                    return true;
                }
            }

            return false;
        }

        protected IList<LdapEntry> Query(LdapConnection connection, string baseDn, string filter, LdapSearchScope scope, params string[] attributes)
        {
            var results = connection.Search(baseDn, filter, attributes, scope);
            return results;
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