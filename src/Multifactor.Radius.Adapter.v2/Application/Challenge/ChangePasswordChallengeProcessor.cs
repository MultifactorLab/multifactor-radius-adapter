using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Challenge.Interface;
using Multifactor.Radius.Adapter.v2.Domain.Auth;
using Multifactor.Radius.Adapter.v2.Domain.Challenge;
using Multifactor.Radius.Adapter.v2.Infrastructure.Cache;
using Multifactor.Radius.Adapter.v2.Infrastructure.Ldap.Dto;
using Multifactor.Radius.Adapter.v2.Infrastructure.Ldap.Interface;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;
using Multifactor.Radius.Adapter.v2.Infrastructure.Security;

namespace Multifactor.Radius.Adapter.v2.Application.Challenge;

public class ChangePasswordChallengeProcessor : IChallengeProcessor
{
    private readonly ICacheService _cache;
    private readonly ILdapProfileService _ldapService;
    private readonly ILogger<ChangePasswordChallengeProcessor> _logger;

    public ChallengeType ChallengeType => ChallengeType.PasswordChange;

    public ChangePasswordChallengeProcessor(
        ICacheService cache,
        ILdapProfileService ldapService,
        ILogger<ChangePasswordChallengeProcessor> logger)
    {
        _cache = cache;
        _ldapService = ldapService;
        _logger = logger;
    }

    public ChallengeIdentifier AddChallengeContext(RadiusPipelineExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        
        if (string.IsNullOrWhiteSpace(context.Passphrase.Password))
            throw new InvalidOperationException("User password is required.");
        
        if (string.IsNullOrWhiteSpace(context.MustChangePasswordDomain))
            throw new InvalidOperationException("Domain is required.");
        
        var encryptedPassword = DataProtectionService.Protect(
            context.ApiCredential.Pwd, 
            context.Passphrase.Password);

        var passwordRequest = new PasswordChangeRequest
        {
            Domain = context.MustChangePasswordDomain,
            CurrentPasswordEncryptedData = encryptedPassword
        };

        _cache.Set(passwordRequest.Id, passwordRequest, DateTimeOffset.UtcNow.AddMinutes(5));
        _logger.LogInformation("Password change state: {RequestId}", passwordRequest.Id);
        
        context.ResponseInformation.State = passwordRequest.Id;
        context.ResponseInformation.ReplyMessage = "Please change password to continue. Enter new password: ";
        
        return new ChallengeIdentifier(context.ClientConfigurationName, context.ResponseInformation.State);
    }

    public bool HasChallengeContext(ChallengeIdentifier identifier) => 
        _cache.TryGetValue<object>(identifier.RequestId, out _);

    public async Task<ChallengeStatus> ProcessChallengeAsync(
        ChallengeIdentifier identifier, 
        RadiusPipelineExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        
        var passwordChangeRequest = GetPasswordChangeRequest(identifier.RequestId);
        if (passwordChangeRequest == null)
            return ChallengeStatus.Accept;
        
        if (string.IsNullOrWhiteSpace(context.Passphrase.Raw))
            return HandleEmptyPassword(context);

        if (string.IsNullOrWhiteSpace(passwordChangeRequest.NewPasswordEncryptedData))
            return HandleFirstPasswordEntry(context, passwordChangeRequest);
        
        return await HandlePasswordConfirmationAsync(context, passwordChangeRequest);
    }

    private PasswordChangeRequest? GetPasswordChangeRequest(string id)
    {
        _cache.TryGetValue(id, out PasswordChangeRequest? passwordChangeRequest);
        return passwordChangeRequest;
    }

    private ChallengeStatus HandleEmptyPassword(RadiusPipelineExecutionContext context)
    {
        context.ResponseInformation.ReplyMessage = "Password is empty";
        context.AuthenticationState.FirstFactorStatus = AuthenticationStatus.Reject;
        return ChallengeStatus.Reject;
    }

    private ChallengeStatus HandleFirstPasswordEntry(
        RadiusPipelineExecutionContext context, 
        PasswordChangeRequest passwordChangeRequest)
    {
        passwordChangeRequest.NewPasswordEncryptedData = DataProtectionService.Protect(
            context.ApiCredential.Pwd, 
            context.Passphrase.Raw!);

        _cache.Set(passwordChangeRequest.Id, passwordChangeRequest, DateTimeOffset.UtcNow.AddMinutes(5));
        context.ResponseInformation.State = passwordChangeRequest.Id;
        context.ResponseInformation.ReplyMessage = "Please repeat new password: ";

        return ChallengeStatus.InProcess;
    }

    private async Task<ChallengeStatus> HandlePasswordConfirmationAsync(
        RadiusPipelineExecutionContext context,
        PasswordChangeRequest passwordChangeRequest)
    {
        var decryptedNewPassword = DataProtectionService.Unprotect(
            context.ApiCredential.Pwd, 
            passwordChangeRequest.NewPasswordEncryptedData);

        if (decryptedNewPassword != context.Passphrase.Raw)
            return HandlePasswordMismatch(context, passwordChangeRequest);

        var request = new ChangeUserPasswordRequest(
            decryptedNewPassword,
            context.UserLdapProfile!,
            context.LdapServerConfiguration!,
            context.LdapSchema!);
        
        var result = await _ldapService.ChangeUserPasswordAsync(request);
            
        _cache.Remove(passwordChangeRequest.Id);
        context.ResponseInformation.State = null;
            
        return result.Success ? ChallengeStatus.Accept : ChallengeStatus.Reject;
    }

    private ChallengeStatus HandlePasswordMismatch(
        RadiusPipelineExecutionContext context,
        PasswordChangeRequest passwordChangeRequest)
    {
        passwordChangeRequest.NewPasswordEncryptedData = null;
        _cache.Set(passwordChangeRequest.Id, passwordChangeRequest, DateTimeOffset.UtcNow.AddMinutes(5));

        context.ResponseInformation.State = passwordChangeRequest.Id;
        context.ResponseInformation.ReplyMessage = "Passwords do not match. Please enter new password: ";
        
        return ChallengeStatus.InProcess;
    }
}