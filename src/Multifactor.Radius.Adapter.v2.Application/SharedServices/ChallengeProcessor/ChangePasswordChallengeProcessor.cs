using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Application.SharedPorts;
using Multifactor.Radius.Adapter.v2.Application.SharedServices.ChallengeProcessor.Models;
using Multifactor.Radius.Adapter.v2.Application.SharedServices.ChallengeProcessor.Ports;
using Multifactor.Radius.Adapter.v2.Application.SharedServices.ChallengeProcessor.Security;

namespace Multifactor.Radius.Adapter.v2.Application.SharedServices.ChallengeProcessor;

internal sealed class ChangePasswordChallengeProcessor : IChallengeProcessor
{
    private readonly IPasswordChangeCache _passCache;
    private readonly IChallengeContextCache _contextCache;
    private readonly IChangePassword _changePassword;
    private readonly ILogger<ChangePasswordChallengeProcessor> _logger;
    public ChangePasswordChallengeProcessor(
        IPasswordChangeCache passCache,
        IChangePassword changePassword,
        IChallengeContextCache contextCache,
        ILogger<ChangePasswordChallengeProcessor> logger)
    {
        _passCache = passCache;
        _changePassword = changePassword;
        _logger = logger;
        _contextCache = contextCache;
    }

    public ChallengeType ChallengeType => ChallengeType.PasswordChange;

    public ChallengeIdentifier AddChallengeContext(RadiusPipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (string.IsNullOrWhiteSpace(context.Passphrase?.Password))
            throw new InvalidOperationException("User password is required.");
        
        if (string.IsNullOrWhiteSpace(context.MustChangePasswordDomain))
            throw new InvalidOperationException("Domain is required.");
        
        var encryptedPassword = ProtectionService.Protect(context.ClientConfiguration.MultifactorSharedSecret, 
            context.Passphrase.Password);
        var passwordRequest = new PasswordChangeCache
        {
            Domain = context.MustChangePasswordDomain,
            CurrentPasswordEncryptedData = encryptedPassword
        };

        _passCache.Set(passwordRequest.Id, passwordRequest, DateTimeOffset.UtcNow.AddMinutes(5));
        _logger.LogInformation("Password change state: \"{PasswordRequestId}\"", passwordRequest.Id);
        context.ResponseInformation.State = passwordRequest.Id;
        context.ResponseInformation.ReplyMessage = "Please change password to continue. Enter new password: ";
        return new ChallengeIdentifier(context.ClientConfiguration.Name, context.ResponseInformation.State);
    }

    public bool HasChallengeContext(ChallengeIdentifier identifier) => _contextCache.TryGetValue(identifier.RequestId, out _);

    public async Task<ChallengeStatus> ProcessChallengeAsync(ChallengeIdentifier identifier, RadiusPipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.LdapProfile);
        ArgumentNullException.ThrowIfNull(context.LdapConfiguration);
        ArgumentNullException.ThrowIfNull(context.LdapSchema);
        
        var passwordChangeRequest = GetPasswordChangeRequest(identifier.RequestId);
        if (passwordChangeRequest == null)
            return ChallengeStatus.Accept;
        
        if (string.IsNullOrWhiteSpace(context.Passphrase.Raw))
        {
            context.ResponseInformation.ReplyMessage = "Password is empty";
            context.FirstFactorStatus = AuthenticationStatus.Reject;
            return ChallengeStatus.Reject;
        }

        if (string.IsNullOrWhiteSpace(passwordChangeRequest.NewPasswordEncryptedData))
            return RepeatPasswordChallenge(context, passwordChangeRequest);
        
        var decryptedNewPassword = ProtectionService.Unprotect(context.ClientConfiguration.MultifactorSharedSecret, passwordChangeRequest.NewPasswordEncryptedData);
        if (decryptedNewPassword != context.Passphrase.Raw)
            return PasswordsNotMatchChallenge(context, passwordChangeRequest);

        var dto = new ChangeUserPasswordDto
        {
            ConnectionString = context.LdapConfiguration.ConnectionString,
            UserName = context.LdapConfiguration.Username,
            Password = context.LdapConfiguration.Password,
            BindTimeoutInSeconds = context.LdapConfiguration.BindTimeoutSeconds,
            LdapSchema = context.LdapSchema,
            DistinguishedName = context.LdapProfile.Dn,
            NewPassword = decryptedNewPassword,
        }; //поменять - должен рабобать с трастами
        
        var success = _changePassword.Execute(dto);
            
        _passCache.Remove(passwordChangeRequest.Id);
        context.ResponseInformation.State = null;
            
        if (success)
            return ChallengeStatus.Accept;

        context.FirstFactorStatus = AuthenticationStatus.Reject;

        return ChallengeStatus.Reject;
    }

    private PasswordChangeCache? GetPasswordChangeRequest(string id)
    {
        _passCache.TryGetValue(id, out PasswordChangeCache? passwordChangeRequest);
        return passwordChangeRequest;
    }

    private ChallengeStatus PasswordsNotMatchChallenge(RadiusPipelineContext request, PasswordChangeCache passwordChangeRequest)
    {
        passwordChangeRequest.NewPasswordEncryptedData = null;

        _passCache.Set(passwordChangeRequest.Id, passwordChangeRequest, DateTimeOffset.UtcNow.AddMinutes(5));

        request.ResponseInformation.State = passwordChangeRequest.Id;
        request.ResponseInformation.ReplyMessage = "Passwords not match. Please enter new password: ";
        
        return ChallengeStatus.InProcess;
    }
    
    private ChallengeStatus RepeatPasswordChallenge(RadiusPipelineContext context, PasswordChangeCache passwordChangeRequest)
    {
        passwordChangeRequest.NewPasswordEncryptedData = ProtectionService.Protect(context.ClientConfiguration.MultifactorSharedSecret, context.Passphrase.Raw!);

        _passCache.Set(passwordChangeRequest.Id, passwordChangeRequest, DateTimeOffset.UtcNow.AddMinutes(5));

        context.ResponseInformation.State = passwordChangeRequest.Id;
        context.ResponseInformation.ReplyMessage = "Please repeat new password: ";

        return ChallengeStatus.InProcess;
    }
}