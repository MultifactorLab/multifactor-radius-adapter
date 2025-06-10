using System.DirectoryServices.Protocols;
using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Connection.LdapConnectionFactory;
using Multifactor.Core.Ldap.LangFeatures;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.Ldap;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;

namespace Multifactor.Radius.Adapter.v2.Core.FirstFactor;

public class LdapFirstFactorProcessor : IFirstFactorProcessor
{
    private LdapConnectionFactory _ldapConnectionFactory;
    private ILogger _logger;

    public AuthenticationSource AuthenticationSource => AuthenticationSource.Ldap;

    public LdapFirstFactorProcessor(LdapConnectionFactory ldapConnectionFactory,
        ILogger<LdapFirstFactorProcessor> logger)
    {
        Throw.IfNull(ldapConnectionFactory, nameof(ldapConnectionFactory));
        Throw.IfNull(logger, nameof(logger));

        _ldapConnectionFactory = ldapConnectionFactory;
        _logger = logger;
    }

    public async Task ProcessFirstFactor(IRadiusPipelineExecutionContext context)
    {
        Throw.IfNull(context, nameof(context));

        var radiusPacket = context.RequestPacket;
        Throw.IfNull(radiusPacket, nameof(radiusPacket));

        if ((context.Settings.LdapServers?.Count ?? 0) <= 0)
            throw new ApplicationException("No Ldap servers configured.");

        if (string.IsNullOrWhiteSpace(radiusPacket.UserName))
        {
            _logger.LogWarning("Can't find User-Name in message id={id} from {host:l}:{port}",
                context.RequestPacket.Identifier, context.RemoteEndpoint.Address, context.RemoteEndpoint.Port);
            Reject(context);
            return;
        }

        var transformedName = UserNameTransformation.Transform(radiusPacket.UserName, context.Settings.UserNameTransformRules.BeforeFirstFactor);

        var pwd = radiusPacket.TryGetUserPassword();
        if (string.IsNullOrWhiteSpace(pwd))
        {
            _logger.LogWarning("No User-Password in message id={id} from {host:l}:{port}",
                context.RequestPacket.Identifier, context.RemoteEndpoint.Address, context.RemoteEndpoint.Port);
            Reject(context);
            return;
        }

        var passphrase = UserPassphrase.Parse(pwd, context.Settings.PreAuthnMode);

        if (string.IsNullOrWhiteSpace(passphrase.Password))
        {
            _logger.LogWarning("Can't parse User-Password in message id={id} from {host:l}:{port}",
                context.RequestPacket.Identifier, context.RemoteEndpoint.Address, context.RemoteEndpoint.Port);
            Reject(context);
            return;
        }

        var availableLdapServer = GetFirstAvailableLdapServer(context, transformedName, passphrase.Password);
        if (availableLdapServer is null)
        {
            Reject(context);
            _logger.LogWarning("No available LDAP servers.");
            return;
        }
        
        _logger.LogInformation("User '{user:l}' credential and status verified successfully at {endpoint:l}", transformedName, availableLdapServer.ConnectionString);
        context.FirstFactorLdapServerConfiguration = availableLdapServer;
        Accept(context);
    }

    private ILdapServerConfiguration? GetFirstAvailableLdapServer(IRadiusPipelineExecutionContext context, string login, string password)
    {
        foreach (var ldapServer in context.Settings.LdapServers!)
        {
            try
            {
                using var connection = GetConnection(
                    ldapServer.ConnectionString,
                    login,
                    password,
                    ldapServer.BindTimeoutInSeconds);
                
                return ldapServer;
            }
            catch (Exception ex)
            {
                if (ex is not LdapException ldapException)
                {
                    _logger.LogError(ex, "Verification user '{user:l}' at {ldapUri:l} failed", login, ldapServer.ConnectionString);
                    continue;
                }

                var info = GetLdapErrorInfo(ldapException);
                if (info != null)
                {
                    ProcessErrorReason(info, context, ldapServer);
                }

                _logger.LogWarning(ldapException, "Verification user '{user:l}' at {ldapUri:l} failed: {dataReason:l}", login, ldapServer.ConnectionString, info?.ReasonText);
            }
        }

        return null;
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
        {
            context.MustChangePasswordDomain = ldapServerConfiguration.ConnectionString;
        }
    }
}