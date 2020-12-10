//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/MultiFactor.Radius.Adapter/blob/master/LICENSE.md

using LdapForNet;
using MultiFactor.Radius.Adapter.Core.Services.Ldap;
using MultiFactor.Radius.Adapter.Server;
using MultiFactor.Radius.Adapter.Services.Ldap;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static LdapForNet.Native.Native;

namespace MultiFactor.Radius.Adapter.Services
{
    /// <summary>
    /// Service to interact with Active Directory
    /// </summary>
    public class ActiveDirectoryService
    {
        private Configuration _configuration;
        private ILogger _logger;

        public ActiveDirectoryService(Configuration configuration, ILogger logger)
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
                _logger.Error($"Empty password provided for user '{userName}'");
                return false;
            }

            var user = LdapIdentity.ParseUser(userName);

            try
            {
                _logger.Debug($"Verifying user '{user.Name}' credential and status at {_configuration.ActiveDirectoryDomain}");

                using (var connection = new LdapConnection())
                {
                    //trust self-signed certificates on ldap server
                    connection.TrustAllCertificates();
                    
                    if (Uri.IsWellFormedUriString(_configuration.ActiveDirectoryDomain, UriKind.Absolute))
                    {
                        connection.Connect(new Uri(_configuration.ActiveDirectoryDomain));
                    }
                    else
                    {
                        connection.Connect(_configuration.ActiveDirectoryDomain, 389);
                    }

                    //do not follow chase referrals
                    connection.SetOption(LdapOption.LDAP_OPT_REFERRALS, IntPtr.Zero);

                    connection.Bind(LdapAuthType.Simple, new LdapCredential 
                    {
                        UserName = user.Name,
                        Password = password
                    });

                    var domain = WhereAmI(connection);

                    _logger.Information($"User '{user.Name}' credential and status verified successfully in {domain.Name}");

                    var isProfileLoaded = LoadProfile(connection, domain, user, out var profile);
                    if (!isProfileLoaded)
                    {
                        return false;
                    }

                    var checkGroupMembership = !string.IsNullOrEmpty(_configuration.ActiveDirectoryGroup);
                    //user must be member of security group
                    if (checkGroupMembership)
                    {
                        var isMemberOf = IsMemberOf(connection, profile.BaseDn, user, _configuration.ActiveDirectoryGroup);

                        if (!isMemberOf)
                        {
                            _logger.Warning($"User '{user.Name}' is not member of '{_configuration.ActiveDirectoryGroup}' group in {profile.BaseDn.Name}");
                            return false;
                        }

                        _logger.Debug($"User '{user.Name}' is member of '{_configuration.ActiveDirectoryGroup}' group in {profile.BaseDn.Name}");
                    }

                    var onlyMembersOfGroupMustProcess2faAuthentication = !string.IsNullOrEmpty(_configuration.ActiveDirectory2FaGroup);
                    //only users from group must process 2fa
                    if (onlyMembersOfGroupMustProcess2faAuthentication)
                    {
                        var isMemberOf = IsMemberOf(connection, profile.BaseDn, user, _configuration.ActiveDirectory2FaGroup);

                        if (isMemberOf)
                        {
                            _logger.Debug($"User '{user.Name}' is member of '{_configuration.ActiveDirectory2FaGroup}' in {profile.BaseDn.Name}");
                        }
                        else
                        {
                            _logger.Information($"User '{user.Name}' is not member of '{_configuration.ActiveDirectory2FaGroup}' in {profile.BaseDn.Name}");
                            request.Bypass2Fa = true;
                        }
                    }

                    //check groups membership for radius reply conditional attributes
                    foreach (var attribute in _configuration.RadiusReplyAttributes)
                    {
                        foreach (var value in attribute.Value.Where(val => val.UserGroupCondition != null))
                        {
                            if (IsMemberOf(connection, profile.BaseDn, user, value.UserGroupCondition))
                            {
                                _logger.Information($"User '{user.Name}' is member of '{value.UserGroupCondition}' in {profile.BaseDn.Name}. Adding attribute '{attribute.Key}:{value.Value}' to reply");
                                request.UserGroups.Add(value.UserGroupCondition);
                            }
                            else
                            {
                                _logger.Debug($"User '{user.Name}' is not member of '{value.UserGroupCondition}' in {profile.BaseDn.Name}");
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
                        _logger.Warning($"Verification user '{user.Name}' at {_configuration.ActiveDirectoryDomain} failed: {dataReason}");
                        return false;
                    }
                }

                _logger.Error(lex, $"Verification user '{user.Name}' at {_configuration.ActiveDirectoryDomain} failed");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Verification user '{user.Name}' at {_configuration.ActiveDirectoryDomain} failed");
            }

            return false;
        }

        private LdapIdentity WhereAmI(LdapConnection connection)
        {
            var result = Query(connection, null, "(objectclass=*)", LdapSearchScope.LDAP_SCOPE_BASEOBJECT, "defaultNamingContext").SingleOrDefault();
            if (result == null)
            {
                throw new InvalidOperationException($"Unable to query {_configuration.ActiveDirectoryDomain} for current user");
            }

            var defaultNamingContext = result.DirectoryAttributes["defaultNamingContext"].GetValue<string>();

            return new LdapIdentity { Name = defaultNamingContext, Type = IdentityType.DistinguishedName };
        }

        private bool LoadProfile(LdapConnection connection, LdapIdentity domain, LdapIdentity user, out LdapProfile profile)
        {
            profile = null;

            var attributes = new[] { "DistinguishedName", "mail", "telephoneNumber", "mobile" };
            var searchFilter = $"(&(objectClass=user)({user.TypeName}={user.Name}))";

            _logger.Debug($"Querying user '{user.Name}' in {domain.Name}");

            var response = Query(connection, domain.Name, searchFilter, LdapSearchScope.LDAP_SCOPE_SUB, attributes);

            var entry = response.SingleOrDefault();
            if (entry == null)
            {
                _logger.Error($"Unable to find user '{user.Name}' in {domain.Name}");
                return false;
            }

            profile = new LdapProfile
            {
                BaseDn = LdapIdentity.BaseDn(entry.Dn),
                DistinguishedName = entry.Dn,
            };

            var attrs = entry.DirectoryAttributes;
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

            _logger.Debug($"User '{user.Name}' profile loaded: {profile.DistinguishedName}");

            return true;
        }

        private bool IsMemberOf(LdapConnection connection, LdapIdentity domain, LdapIdentity user, string groupName)
        {
            var isValidGroup = IsValidGroup(connection, domain, groupName, out var group);

            if (!isValidGroup)
            {
                _logger.Warning($"Security group '{groupName}' not exists in {domain.Name}");
                return false;
            }

            var searchFilter = $"(&({user.TypeName}={user.Name})(memberOf:1.2.840.113556.1.4.1941:={group.Name}))";
            var response = Query(connection, domain.Name, searchFilter, LdapSearchScope.LDAP_SCOPE_SUB, "DistinguishedName");

            return response.Any();
        }

        private bool IsValidGroup(LdapConnection connection, LdapIdentity domain, string groupName, out LdapIdentity validatedGroup)
        {
            validatedGroup = null;

            var group = LdapIdentity.ParseGroup(groupName);
            var searchFilter = $"(&(objectCategory=group)({group.TypeName}={group.Name}))";
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

        private IList<LdapEntry> Query(LdapConnection connection, string baseDn, string filter, LdapSearchScope scope, params string[] attributes)
        {
            //var c = new LdapSearchConstraints()
            //{
            //    ReferralFollowing = true,
            //};

            //c.setReferralHandler(new LdapBindHandlerImpl());

            //var results = connection.Search(baseDn, scope, filter, attributes, false, new LdapSearchConstraints(connection.Constraints)
            //{
            //    ReferralFollowing = true,
            //});
            var results = connection.Search(baseDn, filter, attributes, scope);
            return results;
        }

        private string ExtractErrorReason(string errorMessage)
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