using System.DirectoryServices.Protocols;
using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.LangFeatures;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.Ldap;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;
using ILdapConnectionFactory = Multifactor.Radius.Adapter.v2.Core.Ldap.ILdapConnectionFactory;

namespace Multifactor.Radius.Adapter.v2.Core.FirstFactor;

public class LdapFirstFactorProcessor : IFirstFactorProcessor
{
    private ILdapConnectionFactory _ldapConnectionFactory;
    private ILogger _logger;

    public AuthenticationSource AuthenticationSource => AuthenticationSource.Ldap;

    public LdapFirstFactorProcessor(ILdapConnectionFactory ldapConnectionFactory,
        ILogger<LdapFirstFactorProcessor> logger)
    {
        Throw.IfNull(ldapConnectionFactory, nameof(ldapConnectionFactory));
        Throw.IfNull(logger, nameof(logger));

        _ldapConnectionFactory = ldapConnectionFactory;
        _logger = logger;
    }

    public Task ProcessFirstFactor(IRadiusPipelineExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        var radiusPacket = context.RequestPacket;
        Throw.IfNull(radiusPacket, nameof(radiusPacket));

        if (context.LdapServerConfiguration is null)
            throw new InvalidOperationException("No Ldap servers configured.");

        if (string.IsNullOrWhiteSpace(radiusPacket.UserName))
        {
            _logger.LogWarning("Can't find User-Name in message id={id} from {host:l}:{port}", radiusPacket.Identifier, context.RemoteEndpoint.Address, context.RemoteEndpoint.Port);
            Reject(context);
            return Task.CompletedTask;
        }

        var transformedName = UserNameTransformation.Transform(radiusPacket.UserName, context.UserNameTransformRules.BeforeFirstFactor);

        var passphrase = context.Passphrase;
        if (string.IsNullOrWhiteSpace(passphrase.Raw))
        {
            _logger.LogWarning("No User-Password in message id={id} from {host:l}:{port}", radiusPacket.Identifier, context.RemoteEndpoint.Address, context.RemoteEndpoint.Port);
            Reject(context);
            return Task.CompletedTask;
        }
        
        if (string.IsNullOrWhiteSpace(passphrase.Password))
        {
            _logger.LogWarning("Can't parse User-Password in message id={id} from {host:l}:{port}", radiusPacket.Identifier, context.RemoteEndpoint.Address, context.RemoteEndpoint.Port);
            Reject(context);
            return Task.CompletedTask;
        }

        var isValid = ValidateUserCredentials(context, transformedName, passphrase.Password);
        if (!isValid)
        {
            Reject(context);
            return Task.CompletedTask;
        }

        _logger.LogInformation("User '{user:l}' credential and status verified successfully at {endpoint:l}", transformedName, context.LdapServerConfiguration.ConnectionString);
        Accept(context);
        return Task.CompletedTask;
    }

    private bool ValidateUserCredentials(
        IRadiusPipelineExecutionContext context,
        string login,
        string password)
    {
        var serverConfig = context.LdapServerConfiguration;
        try
        {
            using var connection = GetConnection(
                serverConfig.ConnectionString,
                login,
                password,
                serverConfig.BindTimeoutInSeconds);

            return true;
        }
        catch (Exception ex)
        {
            if (ex is not LdapException ldapException)
            {
                _logger.LogError(ex, "Verification user '{user:l}' at {ldapUri:l} failed", login, serverConfig.ConnectionString);
                return false;
            }

            var info = GetLdapErrorInfo(ldapException);
            if (info != null)
                ProcessErrorReason(info, context, serverConfig);

            _logger.LogWarning(ldapException, "Verification user '{user:l}' at {ldapUri:l} failed: {dataReason:l}", login, serverConfig.ConnectionString, info?.ReasonText);
        }

        return false;
    }

    private ILdapConnection GetConnection(string connectionString, string userName, string password,
        int bindTimeoutInSeconds)
    {
        var connectionOptions = new LdapConnectionOptions(
            new LdapConnectionString(connectionString),
            AuthType.Basic,
            userName,
            password,
            TimeSpan.FromSeconds(bindTimeoutInSeconds));

        return _ldapConnectionFactory.CreateConnection(connectionOptions);
    }

    private void Reject(IRadiusPipelineExecutionContext context)
    {
        context.AuthenticationState.FirstFactorStatus = AuthenticationStatus.Reject;
    }

    private void Accept(IRadiusPipelineExecutionContext context)
    {
        context.AuthenticationState.FirstFactorStatus = AuthenticationStatus.Accept;
    }

    private LdapErrorReasonInfo? GetLdapErrorInfo(LdapException exception)
    {
        if (string.IsNullOrWhiteSpace(exception.Message))
            return null;
        var reason = LdapErrorReasonInfo.Create(exception.Message);
        return reason;
    }

    private void ProcessErrorReason(LdapErrorReasonInfo errorInfo, IRadiusPipelineExecutionContext context, ILdapServerConfiguration ldapServerConfiguration)
    {
        if (errorInfo.Flags.HasFlag(LdapErrorFlag.MustChangePassword))
            context.MustChangePasswordDomain = ldapServerConfiguration.ConnectionString;
    }
}