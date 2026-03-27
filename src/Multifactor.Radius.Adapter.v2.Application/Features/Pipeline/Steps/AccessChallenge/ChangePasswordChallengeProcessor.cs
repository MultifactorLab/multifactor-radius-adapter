using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.PasswordChange.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.PasswordChange.Ports;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps.AccessChallenge.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps.AccessChallenge.Models.Enums;
using Multifactor.Radius.Adapter.v2.Application.Security;
using Multifactor.Radius.Adapter.v2.Application.SharedPorts;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps.AccessChallenge;

public class ChangePasswordChallengeProcessor : IChallengeProcessor
{
    private readonly ICacheService _cache;
    private readonly IChangePassword _changePassword;
    private readonly ILogger<ChangePasswordChallengeProcessor> _logger;

    public ChangePasswordChallengeProcessor(
        ICacheService cache,
        IChangePassword changePassword,
        ILogger<ChangePasswordChallengeProcessor> logger)
    {
        _cache = cache;
        _changePassword = changePassword;
        _logger = logger;
    }

    public ChallengeType ChallengeType => ChallengeType.PasswordChange;

    public ChallengeIdentifier AddChallengeContext(RadiusPipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (string.IsNullOrWhiteSpace(context.Passphrase?.Password))
            throw new InvalidOperationException("User password is required.");
        
        if (string.IsNullOrWhiteSpace(context.MustChangePasswordDomain))
            throw new InvalidOperationException("Domain is required.");
        
        var encryptedPassword = ProtectionService.Protect(context.ClientConfiguration.MultifactorSharedSecret, context.Passphrase.Password);

        var passwordRequest = new PasswordChangeCache
        {
            Domain = context.MustChangePasswordDomain,
            CurrentPasswordEncryptedData = encryptedPassword
        };

        _cache.Set(passwordRequest.Id, passwordRequest, DateTimeOffset.UtcNow.AddMinutes(5));
        _logger.LogInformation("Password change state: \"{PasswordRequestId}\"", passwordRequest.Id);
        context.ResponseInformation.State = passwordRequest.Id;
        context.ResponseInformation.ReplyMessage = "Please change password to continue. Enter new password: ";
        return new ChallengeIdentifier(context.ClientConfiguration.Name, context.ResponseInformation.State);
    }

    public bool HasChallengeContext(ChallengeIdentifier identifier) => _cache.TryGetValue<object>(identifier.RequestId, out _);

    public Task<ChallengeStatus> ProcessChallengeAsync(ChallengeIdentifier identifier, RadiusPipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.LdapProfile);
        ArgumentNullException.ThrowIfNull(context.LdapConfiguration);
        ArgumentNullException.ThrowIfNull(context.LdapSchema);
        
        var passwordChangeRequest = GetPasswordChangeRequest(identifier.RequestId);
        if (passwordChangeRequest == null)
            return Task.FromResult(ChallengeStatus.Accept);
        
        if (string.IsNullOrWhiteSpace(context.Passphrase.Raw))
        {
            context.ResponseInformation.ReplyMessage = "Password is empty";
            context.FirstFactorStatus = AuthenticationStatus.Reject;
            return Task.FromResult(ChallengeStatus.Reject);
        }

        if (string.IsNullOrWhiteSpace(passwordChangeRequest.NewPasswordEncryptedData))
            return Task.FromResult(RepeatPasswordChallenge(context, passwordChangeRequest));
        
        var decryptedNewPassword = ProtectionService.Unprotect(context.ClientConfiguration.MultifactorSharedSecret, passwordChangeRequest.NewPasswordEncryptedData);
        if (decryptedNewPassword != context.Passphrase.Raw)
            return Task.FromResult(PasswordsNotMatchChallenge(context, passwordChangeRequest));

        var dto = new ChangeUserPasswordDto
        {
            ConnectionString = context.LdapConfiguration.ConnectionString,
            UserName = context.LdapConfiguration.Username,
            Password = context.LdapConfiguration.Password,
            BindTimeoutInSeconds = context.LdapConfiguration.BindTimeoutSeconds,
            LdapSchema = context.LdapSchema,
            DistinguishedName = context.LdapProfile.Dn,
            NewPassword = decryptedNewPassword,
        };
        
        var success = _changePassword.Execute(dto);
            
        _cache.Remove(passwordChangeRequest.Id);
        context.ResponseInformation.State = null;
            
        if (success)
            return Task.FromResult(ChallengeStatus.Accept);

        context.FirstFactorStatus = AuthenticationStatus.Reject;

        return Task.FromResult(ChallengeStatus.Reject);
    }

    private PasswordChangeCache? GetPasswordChangeRequest(string id)
    {
        _cache.TryGetValue(id, out PasswordChangeCache? passwordChangeRequest);
        return passwordChangeRequest;
    }

    private ChallengeStatus PasswordsNotMatchChallenge(RadiusPipelineContext request, PasswordChangeCache passwordChangeRequest)
    {
        passwordChangeRequest.NewPasswordEncryptedData = null;

        _cache.Set(passwordChangeRequest.Id, passwordChangeRequest, DateTimeOffset.UtcNow.AddMinutes(5));

        request.ResponseInformation.State = passwordChangeRequest.Id;
        request.ResponseInformation.ReplyMessage = "Passwords not match. Please enter new password: ";
        
        return ChallengeStatus.InProcess;
    }
    
    private ChallengeStatus RepeatPasswordChallenge(RadiusPipelineContext context, PasswordChangeCache passwordChangeRequest)
    {
        passwordChangeRequest.NewPasswordEncryptedData = ProtectionService.Protect(context.ClientConfiguration.MultifactorSharedSecret, context.Passphrase.Raw!);

        _cache.Set(passwordChangeRequest.Id, passwordChangeRequest, DateTimeOffset.UtcNow.AddMinutes(5));

        context.ResponseInformation.State = passwordChangeRequest.Id;
        context.ResponseInformation.ReplyMessage = "Please repeat new password: ";

        return ChallengeStatus.InProcess;
    }
}