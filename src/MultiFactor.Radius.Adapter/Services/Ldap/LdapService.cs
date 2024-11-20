//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using LdapForNet;
using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Core.Exceptions;
using MultiFactor.Radius.Adapter.Core.Framework.Context;
using MultiFactor.Radius.Adapter.Core.Services.Ldap;
using MultiFactor.Radius.Adapter.Services.Ldap.Connection;
using MultiFactor.Radius.Adapter.Services.Ldap.Profile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static LdapForNet.Native.Native;

namespace MultiFactor.Radius.Adapter.Services.Ldap;

public interface ILdapService
{
    Task<PasswordChangeResponse> ChangeUserPasswordAsync(string domain, string oldPassword, string newPassword, RadiusContext context);
    Task VerifyCredential(string userName, string password, string ldapUri, RadiusContext context);
    Task<bool> VerifyMembership(string userName, string password, string ldapUri, RadiusContext context);
}

/// <summary>
/// Service to interact with LDAP server
/// </summary>
public class LdapService : ILdapService
{
    private readonly ProfileLoader _profileLoader;
    private readonly ILogger<LdapService> _logger;
    private readonly ILoggerFactory _loggerFactory;

    protected virtual LdapNames Names => new(LdapServerType.Generic);

    public LdapService(ProfileLoader profileLoader,
        ILogger<LdapService> logger,
        ILoggerFactory loggerFactory)
    {
        _profileLoader = profileLoader;
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Verify User Status and Policy against Active Directory
    /// </summary>
    public async Task<bool> VerifyMembership(string userName, string password, string ldapUri, RadiusContext context)
    {
        if (string.IsNullOrEmpty(userName))
        {
            throw new ArgumentNullException(nameof(userName));
        }

        if (string.IsNullOrEmpty(ldapUri))
        {
            throw new ArgumentNullException(nameof(ldapUri));
        }

        var user = LdapIdentity.ParseUser(userName);

        var formatter = new BindIdentityFormatter(context.Configuration);
        var bindDn = formatter.FormatIdentity(user, ldapUri);

        try
        {
            using var connection = LdapConnectionAdapter.CreateAsync(
                ldapUri,
                user,
                _loggerFactory.CreateLogger<LdapConnectionAdapter>());

            await connection.BindAsync(bindDn, password);

            var profile = await _profileLoader.LoadAsync(context.Configuration, connection, user);

            //user must be member of security group
            if (context.Configuration.ActiveDirectoryGroups.Any())
            {
                var accessGroup =
                    context.Configuration.ActiveDirectoryGroups.FirstOrDefault(group => IsMemberOf(profile, group));
                if (accessGroup != null)
                {
                    _logger.LogDebug("User '{user:l}' is a member of the access group '{group:l}' in {domain:l}",
                        user.Name, accessGroup.Trim(), profile.BaseDn.Name);
                }
                else
                {
                    _logger.LogWarning(
                        "User '{user:l}' is not a member of any access group ({accGroups:l}) in '{domain:l}'",
                        user.Name, string.Join(", ", context.Configuration.ActiveDirectoryGroups), profile.BaseDn.Name);
                    return false;
                }
            }

            //only users from group must process 2fa
            if (context.Configuration.ActiveDirectory2FaGroup.Any())
            {
                var mfaGroup =
                    context.Configuration.ActiveDirectory2FaGroup.FirstOrDefault(group => IsMemberOf(profile, group));
                if (mfaGroup != null)
                {
                    _logger.LogDebug("User '{user:l}' is a member of the 2FA group '{group:l}' in '{domain:l}'",
                        user.Name, mfaGroup.Trim(), profile.BaseDn.Name);
                }
                else
                {
                    _logger.LogInformation(
                        "User '{user:l}' is not a member of any 2FA group ({groups:l}) in '{domain:l}'",
                        user.Name, string.Join(", ", context.Configuration.ActiveDirectory2FaGroup),
                        profile.BaseDn.Name);
                    context.SetSecondFactorAuth(AuthenticationCode.Bypass);
                }
            }

            //users from group must not process 2fa
            if (context.Authentication.SecondFactor != AuthenticationCode.Bypass &&
                context.Configuration.ActiveDirectory2FaBypassGroup.Any())
            {
                var bypassGroup =
                    context.Configuration.ActiveDirectory2FaBypassGroup.FirstOrDefault(group =>
                        IsMemberOf(profile, group));

                if (bypassGroup != null)
                {
                    _logger.LogInformation(
                        "User '{user:l}' is a member of the 2FA bypass group '{group:l}' in '{domain:l}'",
                        user.Name, bypassGroup.Trim(), profile.BaseDn.Name);
                    context.SetSecondFactorAuth(AuthenticationCode.Bypass);
                }
                else
                {
                    _logger.LogDebug(
                        "User '{user:l}' is not a member of any 2FA bypass group ({groups:l}) in '{domain:l}'",
                        user.Name, string.Join(", ", context.Configuration.ActiveDirectory2FaBypassGroup),
                        profile.BaseDn.Name);
                }
            }

            context.UpdateProfile(profile);

            return true;
        }
        catch (LdapUserNotFoundException ex)
        {
            _logger.LogWarning(ex, "Verification user '{user:l}' at '{ldapUri:l}' failed: {msg:l}", user.Name, ldapUri,
                ex.Message);
            return false;
        }
        catch (LdapException lex)
        {
            if (lex.Message != null)
            {
                var reason = LdapErrorReasonInfo.Create(lex.Message);
                if (reason.Flags.HasFlag(LdapErrorFlag.MustChangePassword))
                {
                    context.SetMustChangePassword(ldapUri);
                }

                if (reason.Reason != LdapErrorReason.UnknownError)
                {
                    _logger.LogWarning(lex, "Verification user '{user:l}' at {ldapUri:l} failed: {dataReason:l}",
                        user.Name, ldapUri, reason.ReasonText);
                    return false;
                }
            }

            _logger.LogError(lex, "Verification user '{user:l}' at {ldapUri:l} failed", user.Name, ldapUri);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Verification user '{user:l}' at {ldapUri:l} failed", user.Name, ldapUri);
        }

        return false;
    }

    public async Task VerifyCredential(string userName, string password, string ldapUri, RadiusContext context)
    {
        if (string.IsNullOrEmpty(userName))
        {
            throw new ArgumentNullException(nameof(userName));
        }

        if (string.IsNullOrEmpty(password))
        {
            _logger.LogError("Empty password provided for user '{user:l}'", userName);
            return;
        }

        if (string.IsNullOrEmpty(ldapUri))
        {
            throw new ArgumentNullException(nameof(ldapUri));
        }

        var user = LdapIdentity.ParseUser(userName);

        var formatter = new BindIdentityFormatter(context.Configuration);
        var bindDn = formatter.FormatIdentity(user, ldapUri);
        _logger.LogDebug(
            "Verifying user '{user:l}' credential and status at '{ldapUri:l}'",
            bindDn,
            ldapUri);

        using var connection = LdapConnectionAdapter.CreateAsync(
            ldapUri,
            user,
            _loggerFactory.CreateLogger<LdapConnectionAdapter>(),
            context.Configuration.LdapBindTimeout);
        
        await WaitTaskWithTimeout(connection.BindAsync(bindDn, password), context.Configuration.LdapBindTimeout);

        var domain = await connection.WhereAmIAsync();

        _logger.LogInformation(
            "User '{user:l}' credential and status verified successfully in '{domain:l}'",
            user.Name,
            domain.Name);
    }

    public async Task<PasswordChangeResponse> ChangeUserPasswordAsync(
        string domain,
        string oldPassword,
        string newPassword,
        RadiusContext context)
    {
        try
        {
            var user = LdapIdentity.ParseUser(context.UserName);
            using var connection = LdapConnectionAdapter.CreateAsTechnicalAccAsync(
                domain,
                context.Configuration,
                _loggerFactory.CreateLogger<LdapConnectionAdapter>());

            var formatter = new BindIdentityFormatter(context.Configuration);
            var serviceUser = LdapIdentity.ParseUser(context.Configuration.ServiceAccountUser);
            await connection.BindAsync(formatter.FormatIdentity(serviceUser, domain), context.Configuration.ServiceAccountPassword);
           
            var profile = await _profileLoader.LoadAsync(context.Configuration, connection, user);
            var request = BuildPasswordChangeRequest(profile.DistinguishedName, oldPassword, newPassword);
            var response = await connection.SendRequestAsync(request);
            
            if (response.ResultCode != ResultCode.Success)
            {
                _logger.LogError($"Password change command error: {response.ErrorMessage}");
                return new PasswordChangeResponse() { Success = false, Message = response.ErrorMessage };
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            return new PasswordChangeResponse() { Success = false, Message = e.Message };
        }
        
        return new PasswordChangeResponse() { Success = true };
    }

    protected async Task<LdapIdentity> WhereAmI(string host, LdapConnection connection)
    {
        var queryResult = await Query(connection, "", "(objectclass=*)", LdapSearchScope.LDAP_SCOPE_BASEOBJECT,
            "defaultNamingContext");
        var result = queryResult.SingleOrDefault() ??
                     throw new InvalidOperationException($"Unable to query {host} for current user");
        var defaultNamingContext = result.DirectoryAttributes["defaultNamingContext"].GetValue<string>();

        return new LdapIdentity(defaultNamingContext, IdentityType.DistinguishedName);
    }

    protected bool IsMemberOf(LdapProfile profile, string group)
    {
        return profile.MemberOf.Any(g => g.ToLower() == group.ToLower().Trim());
    }

    protected async Task<IList<LdapEntry>> Query(LdapConnection connection, string baseDn, string filter,
        LdapSearchScope scope, params string[] attributes)
    {
        var results = await connection.SearchAsync(baseDn, filter, attributes, scope);
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
                    return "AD invalid credentials";
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

    private static async Task WaitTaskWithTimeout(Task targetTask, TimeSpan timeout)
    {
        using var timeoutCancellationTokenSource = new CancellationTokenSource();
        using var timeoutTask = Task.Delay(timeout, timeoutCancellationTokenSource.Token);
        using var completedTask = await Task.WhenAny(targetTask, timeoutTask);
        if (completedTask == targetTask)
        {
            timeoutCancellationTokenSource.Cancel();
            await targetTask;
        }
        else
        {
            throw new TimeoutException("The operation timed out after " + timeout.TotalSeconds + " seconds");
        }
    }

    private ModifyRequest BuildPasswordChangeRequest(string dn, string oldPassword, string newPassword)
    {
        var oldPasswordAttribute = new DirectoryModificationAttribute
        {
            Name = "unicodePwd",
            LdapModOperation = LdapModOperation.LDAP_MOD_DELETE
        };

        oldPasswordAttribute.Add(Encoding.Unicode.GetBytes($"\"{oldPassword}\""));

        var newPasswordAttribute = new DirectoryModificationAttribute
        {
            Name = "unicodePwd",
            LdapModOperation = LdapModOperation.LDAP_MOD_ADD
        };

        newPasswordAttribute.Add(Encoding.Unicode.GetBytes($"\"{newPassword}\""));

        return new ModifyRequest(dn, oldPasswordAttribute, newPasswordAttribute);
    }
}