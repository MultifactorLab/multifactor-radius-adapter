using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Core;
using MultiFactor.Radius.Adapter.Core.Framework.Context;
using MultiFactor.Radius.Adapter.Services;
using MultiFactor.Radius.Adapter.Services.Ldap;

namespace MultiFactor.Radius.Adapter.Server.Pipeline.AccessChallenge;

public class ChangePasswordChallengeProcessor : IChallengeProcessor
{
    private readonly IMemoryCache _cache;
    private readonly ILdapService _ldapService;
    private readonly DataProtectionService _dataProtectionService;
    private readonly ILogger<ChangePasswordChallengeProcessor> _logger;
    public ChallengeType ChallengeType => ChallengeType.PasswordChange;
    
    public ChangePasswordChallengeProcessor(IMemoryCache memoryCache, ILdapService ldapService, DataProtectionService dataProtectionService, ILogger<ChangePasswordChallengeProcessor> logger)
    {
        _cache = memoryCache;
        _ldapService = ldapService;
        _dataProtectionService = dataProtectionService;
        _logger = logger;
    }
    
    public ChallengeIdentifier AddChallengeContext(RadiusContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var encryptedPassword = _dataProtectionService.Protect(context.Passphrase.Password);
        
        var passwordRequest = new PasswordChangeRequest()
        {
            Domain = context.MustChangePasswordDomain,
            CurrentPasswordEncryptedData = encryptedPassword
        };
        
        _cache.Set(passwordRequest.Id, passwordRequest, DateTimeOffset.UtcNow.AddMinutes(5));
        _logger.LogInformation($"Password change state: \"{passwordRequest.Id}\"");
        context.SetMessageState(passwordRequest.Id);
        context.SetReplyMessage("Please change password to continue. Enter new password: ");
        return new ChallengeIdentifier(context.Configuration.Name, context.State);
    }

    public bool HasChallengeContext(ChallengeIdentifier identifier)
    {
        return _cache.TryGetValue(identifier.RequestId, out _);
    }

    public async Task<ChallengeCode> ProcessChallengeAsync(ChallengeIdentifier identifier, RadiusContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var passwordChangeRequest = GetPasswordChangeRequest(identifier.RequestId);
        if (passwordChangeRequest == null)
        {
            return ChallengeCode.Accept;
        }
        
        if (string.IsNullOrEmpty(context.Passphrase.Raw))
        {
            context.SetReplyMessage("Password is empty");
            context.SetFirstFactorAuth(AuthenticationCode.Reject);
            return ChallengeCode.Reject;
        }
        
        if (passwordChangeRequest.NewPasswordEncryptedData != null)
        {
            var decryptedNewPassword = _dataProtectionService.Unprotect(passwordChangeRequest.NewPasswordEncryptedData);
            if (decryptedNewPassword != context.Passphrase.Raw)
            {
                return PasswordsNotMatchChallenge(context, passwordChangeRequest);
            }

            var currentPassword = _dataProtectionService.Unprotect(passwordChangeRequest.CurrentPasswordEncryptedData);

            var result = await _ldapService.ChangeUserPasswordAsync(
                passwordChangeRequest.Domain,
                oldPassword: currentPassword,
                newPassword: decryptedNewPassword,
                context);
            
            _cache.Remove(passwordChangeRequest.Id);
            context.SetMessageState(null);
            
            if(result.Success)
            {
                return ChallengeCode.Accept;
            }

            context.SetFirstFactorAuth(AuthenticationCode.Reject);

            return ChallengeCode.Reject;
        }

        return RepeatPasswordChallenge(context, passwordChangeRequest);
    }

    private PasswordChangeRequest GetPasswordChangeRequest(string id)
    {
        return (PasswordChangeRequest)_cache.Get(id);
    }
    
    private ChallengeCode PasswordsNotMatchChallenge(RadiusContext request, PasswordChangeRequest passwordChangeRequest)
    {
        passwordChangeRequest.NewPasswordEncryptedData = null;

        _cache.Set(passwordChangeRequest.Id, passwordChangeRequest, DateTimeOffset.UtcNow.AddMinutes(5));

        request.SetMessageState(passwordChangeRequest.Id);
        request.SetReplyMessage("Passwords not match. Please enter new password: ");
        
        return ChallengeCode.InProcess;
    }
    
    private ChallengeCode RepeatPasswordChallenge(RadiusContext request, PasswordChangeRequest passwordChangeRequest)
    {
        passwordChangeRequest.NewPasswordEncryptedData = _dataProtectionService.Protect(request.Passphrase.Raw);

        _cache.Set(passwordChangeRequest.Id, passwordChangeRequest, DateTimeOffset.UtcNow.AddMinutes(5));

        request.SetMessageState(passwordChangeRequest.Id);
        request.SetReplyMessage("Please repeat new password: ");

        return ChallengeCode.InProcess;
    }
}