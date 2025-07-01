using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;
using Multifactor.Radius.Adapter.v2.Services.DataProtection;
using Multifactor.Radius.Adapter.v2.Services.Ldap;

namespace Multifactor.Radius.Adapter.v2.Core.AccessChallenge;

public class ChangePasswordChallengeProcessor : IChallengeProcessor
{
    private readonly IMemoryCache _cache;
    private readonly ILdapProfileService _ldapService;
    private readonly IDataProtectionService _dataProtectionService;
    private readonly ILogger<ChangePasswordChallengeProcessor> _logger;

    public ChangePasswordChallengeProcessor(
        IMemoryCache memoryCache,
        ILdapProfileService ldapService,
        IDataProtectionService dataProtectionService,
        ILogger<ChangePasswordChallengeProcessor> logger)
    {
        _cache = memoryCache;
        _ldapService = ldapService;
        _dataProtectionService = dataProtectionService;
        _logger = logger;
    }

    public ChallengeType ChallengeType => ChallengeType.PasswordChange;

    public ChallengeIdentifier AddChallengeContext(IRadiusPipelineExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (string.IsNullOrWhiteSpace(context.Passphrase.Password))
            throw new InvalidOperationException("User password is required.");
        
        if (string.IsNullOrWhiteSpace(context.MustChangePasswordDomain))
            throw new InvalidOperationException("Domain is required.");
        
        var encryptedPassword = _dataProtectionService.Protect(context.ApiCredential.Pwd, context.Passphrase.Password);

        var passwordRequest = new PasswordChangeRequest()
        {
            Domain = context.MustChangePasswordDomain,
            CurrentPasswordEncryptedData = encryptedPassword
        };

        _cache.Set(passwordRequest.Id, passwordRequest, DateTimeOffset.UtcNow.AddMinutes(5));
        _logger.LogInformation($"Password change state: \"{passwordRequest.Id}\"");
        context.ResponseInformation.State = passwordRequest.Id;
        context.ResponseInformation.ReplyMessage = "Please change password to continue. Enter new password: ";
        return new ChallengeIdentifier(context.ClientConfigurationName, context.ResponseInformation.State);
    }

    public bool HasChallengeContext(ChallengeIdentifier identifier) => _cache.TryGetValue(identifier.RequestId, out _);

    public async Task<ChallengeStatus> ProcessChallengeAsync(ChallengeIdentifier identifier, IRadiusPipelineExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var passwordChangeRequest = GetPasswordChangeRequest(identifier.RequestId);
        if (passwordChangeRequest == null)
            return ChallengeStatus.Accept;
        
        if (string.IsNullOrWhiteSpace(context.Passphrase.Raw))
        {
            context.ResponseInformation.ReplyMessage = "Password is empty";
            context.AuthenticationState.FirstFactorStatus = AuthenticationStatus.Reject;
            return ChallengeStatus.Reject;
        }

        if (string.IsNullOrWhiteSpace(passwordChangeRequest.NewPasswordEncryptedData))
            return RepeatPasswordChallenge(context, passwordChangeRequest);
        
        var decryptedNewPassword = _dataProtectionService.Unprotect(context.ApiCredential.Pwd, passwordChangeRequest.NewPasswordEncryptedData);
        if (decryptedNewPassword != context.Passphrase.Raw)
            return PasswordsNotMatchChallenge(context, passwordChangeRequest);

        var request = new ChangeUserPasswordRequest(
            decryptedNewPassword,
            context.UserLdapProfile,
            context.LdapServerConfiguration,
            context.LdapSchema!);
        
        var result = await _ldapService.ChangeUserPasswordAsync(request);
            
        _cache.Remove(passwordChangeRequest.Id);
        context.ResponseInformation.State = null;
            
        if (result.Success)
            return ChallengeStatus.Accept;

        context.AuthenticationState.FirstFactorStatus = AuthenticationStatus.Reject;

        return ChallengeStatus.Reject;
    }
    
    private PasswordChangeRequest? GetPasswordChangeRequest(string id) => _cache.Get(id) as PasswordChangeRequest;
    
    private ChallengeStatus PasswordsNotMatchChallenge(IRadiusPipelineExecutionContext request, PasswordChangeRequest passwordChangeRequest)
    {
        passwordChangeRequest.NewPasswordEncryptedData = null;

        _cache.Set(passwordChangeRequest.Id, passwordChangeRequest, DateTimeOffset.UtcNow.AddMinutes(5));

        request.ResponseInformation.State = passwordChangeRequest.Id;
        request.ResponseInformation.ReplyMessage = "Passwords not match. Please enter new password: ";
        
        return ChallengeStatus.InProcess;
    }
    
    private ChallengeStatus RepeatPasswordChallenge(IRadiusPipelineExecutionContext context, PasswordChangeRequest passwordChangeRequest)
    {
        passwordChangeRequest.NewPasswordEncryptedData = _dataProtectionService.Protect(context.ApiCredential.Pwd, context.Passphrase.Raw!);

        _cache.Set(passwordChangeRequest.Id, passwordChangeRequest, DateTimeOffset.UtcNow.AddMinutes(5));

        context.ResponseInformation.State = passwordChangeRequest.Id;
        context.ResponseInformation.ReplyMessage = "Please repeat new password: ";

        return ChallengeStatus.InProcess;
    }
}