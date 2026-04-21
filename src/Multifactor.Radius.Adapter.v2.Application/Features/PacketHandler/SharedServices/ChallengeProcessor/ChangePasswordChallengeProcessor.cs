using System.DirectoryServices.Protocols;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.SharedServices.ChallengeProcessor.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.SharedServices.ChallengeProcessor.Ports;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.SharedServices.ChallengeProcessor.Security;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.SharedServices.ChallengeProcessor;

internal sealed class ChangePasswordChallengeProcessor : IChallengeProcessor
{
    private readonly IPasswordChangeCache _passCache;
    private readonly IChangePassword _changePassword;
    private readonly ILogger<ChangePasswordChallengeProcessor> _logger;
    public ChangePasswordChallengeProcessor(
        IPasswordChangeCache passCache,
        IChangePassword changePassword,
        ILogger<ChangePasswordChallengeProcessor> logger)
    {
        _passCache = passCache;
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
        
        var encryptedPassword = ProtectionService.Protect(context.ClientConfiguration.MultifactorSharedSecret, 
            context.Passphrase.Password);
        var passwordRequest = new PasswordChangeValue
        {
            Domain = context.MustChangePasswordDomain,
            CurrentPasswordEncryptedData = encryptedPassword
        };

        _passCache.Set(passwordRequest.Id, passwordRequest, DateTimeOffset.UtcNow.AddMinutes(10));
        _logger.LogInformation("Password change state: \"{PasswordRequestId}\"", passwordRequest.Id);
        context.ResponseInformation.State = passwordRequest.Id;
        context.ResponseInformation.ReplyMessage = "Please change password to continue. Enter new password: ";
        return new ChallengeIdentifier(context.ClientConfiguration.Name, context.ResponseInformation.State);
    }

    public bool HasChallengeContext(ChallengeIdentifier identifier) => _passCache.TryGetValue(identifier.RequestId, out _);

    public Task<ChallengeStatus> ProcessChallengeAsync(ChallengeIdentifier identifier, RadiusPipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.LdapProfile);
        ArgumentNullException.ThrowIfNull(context.LdapConfiguration);
        
        var passwordChangeRequest = GetPasswordChangeRequest(identifier.RequestId);
        if (passwordChangeRequest == null)
        {
            return Task.FromResult(ChallengeStatus.Accept);
        }
        
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
        
        var userIdentity = new UserIdentity(context.RequestPacket.UserName);
        var domainInfo = context.ForestMetadata?.DetermineForestDomain(userIdentity);
        var connectionString = domainInfo?.ConnectionString ?? context.LdapConfiguration!.ConnectionString;
        var schema = domainInfo?.Schema ?? context.LdapSchema;
        
        var authType = domainInfo?.GetAuthType() ?? AuthType.Basic;
        var userName = context.LdapConfiguration.Username;
        if (authType == AuthType.Negotiate)
        {
            userName = UserIdentity.TransformDnToUpn(context.LdapConfiguration.Username);
        }
        var dto = new ChangeUserPasswordDto
        {
            AuthType = authType,
            ConnectionString = connectionString,
            UserName = userName,
            Password = context.LdapConfiguration.Password,
            BindTimeoutInSeconds = context.LdapConfiguration.BindTimeoutSeconds,
            LdapSchema = schema,
            DistinguishedName = context.LdapProfile.Dn,
            NewPassword = decryptedNewPassword,
        };
        
        var success = _changePassword.Execute(dto);
            
        _passCache.Remove(passwordChangeRequest.Id);
        context.ResponseInformation.State = null;
            
        if (success)
            return Task.FromResult(ChallengeStatus.Accept);

        context.FirstFactorStatus = AuthenticationStatus.Reject;

        return Task.FromResult(ChallengeStatus.Reject);
    }

    private PasswordChangeValue? GetPasswordChangeRequest(string id)
    {
        _passCache.TryGetValue(id, out var passwordChangeRequest);
        return passwordChangeRequest;
    }

    private ChallengeStatus PasswordsNotMatchChallenge(RadiusPipelineContext request, PasswordChangeValue passwordChangeRequest)
    {
        passwordChangeRequest.NewPasswordEncryptedData = null;

        _passCache.Set(passwordChangeRequest.Id, passwordChangeRequest, DateTimeOffset.UtcNow.AddMinutes(5));

        request.ResponseInformation.State = passwordChangeRequest.Id;
        request.ResponseInformation.ReplyMessage = "Passwords not match. Please enter new password: ";
        
        return ChallengeStatus.InProcess;
    }
    
    private ChallengeStatus RepeatPasswordChallenge(RadiusPipelineContext context, PasswordChangeValue passwordChangeRequest)
    {
        passwordChangeRequest.NewPasswordEncryptedData = ProtectionService.Protect(context.ClientConfiguration.MultifactorSharedSecret, context.Passphrase.Raw!);

        _passCache.Set(passwordChangeRequest.Id, passwordChangeRequest, DateTimeOffset.UtcNow.AddMinutes(5));

        context.ResponseInformation.State = passwordChangeRequest.Id;
        context.ResponseInformation.ReplyMessage = "Please repeat new password: ";

        return ChallengeStatus.InProcess;
    }
}