using System.DirectoryServices.Protocols;
using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.FirstFactor.BindNameFormat;
using Multifactor.Radius.Adapter.v2.Core.Ldap;
using Multifactor.Radius.Adapter.v2.Core.Radius;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;
using ILdapConnection = Multifactor.Radius.Adapter.v2.Core.Ldap.ILdapConnection;
using ILdapConnectionFactory = Multifactor.Radius.Adapter.v2.Core.Ldap.ILdapConnectionFactory;

namespace Multifactor.Radius.Adapter.v2.Core.FirstFactor.LdapAuth;

public class PapAuthProcessor : ILdapAuthProcessor
{
    private readonly ILogger<PapAuthProcessor> _logger;
    private ILdapConnectionFactory _ldapConnectionFactory;
    private readonly ILdapBindNameFormatterProvider _ldapBindNameFormatterProvider;

    public AuthenticationType AuthenticationType => AuthenticationType.PAP;
    
    public PapAuthProcessor(ILdapConnectionFactory ldapConnectionFactory, ILdapBindNameFormatterProvider ldapBindNameFormatterProvider, ILogger<PapAuthProcessor> logger)
    {
        _ldapConnectionFactory = ldapConnectionFactory;
        _ldapBindNameFormatterProvider = ldapBindNameFormatterProvider;
        _logger = logger;
    }
    
    public async Task<AuthResult> Auth(IRadiusPipelineExecutionContext context)
    {
        var radiusPacket = context.RequestPacket;
        var passphrase = context.Passphrase;
        
        if (string.IsNullOrWhiteSpace(radiusPacket.UserName))
        {
            _logger.LogWarning("Can't find User-Name in message id={id} from {host:l}:{port}", radiusPacket.Identifier, context.RemoteEndpoint.Address, context.RemoteEndpoint.Port);
            return new AuthResult() { IsSuccess = false };;
        }
        
        if (string.IsNullOrWhiteSpace(passphrase.Raw))
        {
            _logger.LogWarning("No User-Password in message id={id} from {host:l}:{port}", radiusPacket.Identifier, context.RemoteEndpoint.Address, context.RemoteEndpoint.Port);
            return new AuthResult() { IsSuccess = false };
        }
        
        if (string.IsNullOrWhiteSpace(passphrase.Password))
        {
            _logger.LogWarning("Can't parse User-Password in message id={id} from {host:l}:{port}", radiusPacket.Identifier, context.RemoteEndpoint.Address, context.RemoteEndpoint.Port);
            return new AuthResult() { IsSuccess = false };
        }
        
        var transformedName = UserNameTransformation.Transform(radiusPacket.UserName, context.UserNameTransformRules.BeforeFirstFactor);
        var isValid = ValidateUserCredentials(context, transformedName, passphrase.Password);
        await Task.CompletedTask;
        return new AuthResult() { IsSuccess = isValid };
    }
    
    private bool ValidateUserCredentials(
        IRadiusPipelineExecutionContext context,
        string login,
        string password)
    {
        var serverConfig = context.LdapServerConfiguration;
        if (serverConfig is null)
            throw new InvalidOperationException("No Ldap servers configured.");
        
        var bindName = string.Empty;
        
        try
        {
            var ldapImpl = context.LdapSchema!.LdapServerImplementation;
            var formatter = _ldapBindNameFormatterProvider.GetLdapBindNameFormatter(ldapImpl);
            if (formatter is null)
                _logger.LogWarning("No LDAP bind name formatter configured for '{implementation}' implementation.", ldapImpl);

            var formatted = string.Empty;
            if (context.UserLdapProfile is not null)
                formatted = formatter?.FormatName(login, context.UserLdapProfile);
            
            bindName = string.IsNullOrWhiteSpace(formatted) ? login : formatted;
            
            _logger.LogDebug("Use '{name}' for LDAP bind.", bindName);
            
            using var connection = GetConnection(
                serverConfig.ConnectionString,
                bindName,
                password,
                serverConfig.BindTimeoutInSeconds);

            return true;
        }
        catch (Exception ex)
        {
            if (ex is not LdapException ldapException)
            {
                _logger.LogError(ex, "Verification user '{user:l}' at {ldapUri:l} failed", bindName, serverConfig.ConnectionString);
                return false;
            }

            var info = GetLdapErrorInfo(ldapException);
            if (info != null)
                ProcessErrorReason(info, context, serverConfig);

            _logger.LogWarning(ldapException, "Verification user '{user:l}' at {ldapUri:l} failed: {dataReason:l}", bindName, serverConfig.ConnectionString, info?.ReasonText);
        }

        return false;
    }
    
    private ILdapConnection GetConnection(string connectionString, string userName, string password, int bindTimeoutInSeconds)
    {
        var connectionOptions = new LdapConnectionOptions(
            new LdapConnectionString(connectionString),
            AuthType.Basic,
            userName,
            password,
            TimeSpan.FromSeconds(bindTimeoutInSeconds));

        return _ldapConnectionFactory.CreateConnection(connectionOptions);
    }
    
    private LdapErrorReasonInfo? GetLdapErrorInfo(LdapException exception)
    {
        if (string.IsNullOrWhiteSpace(exception.ServerErrorMessage))
            return null;
        var reason = LdapErrorReasonInfo.Create(exception.ServerErrorMessage);
        return reason;
    }

    private void ProcessErrorReason(LdapErrorReasonInfo errorInfo, IRadiusPipelineExecutionContext context, ILdapServerConfiguration ldapServerConfiguration)
    {
        if (errorInfo.Flags.HasFlag(LdapErrorFlag.MustChangePassword))
            context.MustChangePasswordDomain = ldapServerConfiguration.ConnectionString;
    }
}