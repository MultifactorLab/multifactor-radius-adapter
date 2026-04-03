using System.DirectoryServices.Protocols;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Name;
using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.FirstFactor.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.FirstFactor.Ports;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.FirstFactor.Processor;

internal sealed class LdapFirstFactorProcessor : IFirstFactorProcessor
{
    private readonly ICheckConnection _checkConnection;
    private readonly ILogger<LdapFirstFactorProcessor> _logger;
    public AuthenticationSource AuthenticationSource => AuthenticationSource.Ldap;

    public LdapFirstFactorProcessor(ICheckConnection checkConnection,
        ILogger<LdapFirstFactorProcessor> logger)
    {
        _checkConnection = checkConnection;
        _logger = logger;
    }

    public Task Execute(RadiusPipelineContext context) 
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        var radiusPacket = context.RequestPacket;

        #region Validation
        if (context.LdapConfiguration is null)
            throw new InvalidOperationException("No Ldap servers configured.");

        if (string.IsNullOrWhiteSpace(radiusPacket.UserName))
        {
            _logger.LogWarning("Can't find User-Name in message id={id} from {host:l}:{port}",
                radiusPacket.Identifier, context.RequestPacket.RemoteEndpoint?.Address, context.RequestPacket.RemoteEndpoint?.Port);
            Reject(context);
            return Task.CompletedTask;
        }

        var passphrase = context.Passphrase;
        if (string.IsNullOrWhiteSpace(passphrase?.Raw))
        {
            _logger.LogWarning("No User-Password in message id={id} from {host:l}:{port}",
                radiusPacket.Identifier, context.RequestPacket.RemoteEndpoint?.Address, context.RequestPacket.RemoteEndpoint?.Port);
            Reject(context);
            return Task.CompletedTask;
        }

        if (string.IsNullOrWhiteSpace(passphrase.Password))
        {
            _logger.LogWarning("Can't parse User-Password in message id={id} from {host:l}:{port}",
                radiusPacket.Identifier, context.RequestPacket.RemoteEndpoint?.Address, context.RequestPacket.RemoteEndpoint?.Port);
            Reject(context);
            return Task.CompletedTask;
        }
        #endregion

        var userIdentity = new UserIdentity(context.RequestPacket.UserName);
        var domain = context.ForestMetadata?.DetermineForestDomain(userIdentity);
        var formatted = LdapBindNameFormatter.FormatName(context.RequestPacket.UserName!, context.LdapProfile!);
        var connectionString = domain?.ConnectionString ?? context.LdapConfiguration!.ConnectionString;
        var authType = domain is null ? AuthType.Basic : AuthType.Negotiate;
        var isValid = ValidateUserCredentials(context, formatted, passphrase.Password, connectionString, authType);

        if (!isValid)
        {
            _logger.LogWarning("Authentication attempts failed for user '{user}'", radiusPacket.UserName);
            Reject(context);
            return Task.CompletedTask;
        }
        
        _logger.LogInformation("User '{user:l}' credential verified successfully", radiusPacket.UserName);
        Accept(context);
        return Task.CompletedTask;
    }

    private bool ValidateUserCredentials(
        RadiusPipelineContext context,
        string login,
        string password,
        string connectionString,
        AuthType authType)
    {
        var serverConfig = context.LdapConfiguration;
        if (serverConfig is null)
            throw new InvalidOperationException("No Ldap servers configured.");

        try
        {
            _logger.LogDebug("Use '{name}' for LDAP bind.", login);

            var request = new CheckConnectionDto(connectionString, login,
                password, serverConfig.BindTimeoutSeconds, authType);

            return _checkConnection.Execute(request);
        }
        catch (Exception ex)
        {
            if (ex is not LdapException ldapException)
            {
                _logger.LogError(ex, "Verification user '{user:l}' at {ldapUri:l} failed",
                    login, connectionString);
                return false;
            }

            if (CheckLdapException(ldapException, out var reasonText))
                context.MustChangePasswordDomain = connectionString;

            _logger.LogWarning(ldapException, "Verification user '{user:l}' at {ldapUri:l} failed: {dataReason:l}",
                login, connectionString, reasonText);
        }

        return false;
    }
    
    private static void Reject(RadiusPipelineContext context)
    {
        context.FirstFactorStatus = AuthenticationStatus.Reject;
    }

    private static void Accept(RadiusPipelineContext context)
    {
        context.FirstFactorStatus = AuthenticationStatus.Accept;
    }
    
    private static bool CheckLdapException(LdapException exception, out string reasonText)
    {
        if (string.IsNullOrWhiteSpace(exception.ServerErrorMessage))
        {
            reasonText = "UnknownError";
            return false;
        }

        var pattern = @"data ([0-9a-e]{3})";
        var match = Regex.Match(exception.ServerErrorMessage, pattern);

        if (!match.Success || match.Groups.Count != 2)
        {
            reasonText = "UnknownError";
            return false;
        }

        var data = match.Groups[1].Value;
        switch (data)
        {
            case "525":
                reasonText = "UserNotFound";
                break;
            case "52e":
                reasonText = "InvalidCredentials";
                break;
            case "530":
                reasonText = "NotPermittedToLogonAtThisTime";
                break;
            case "531":
                reasonText = "NotPermittedToLogonAtThisWorkstation";
                break;
            case "532":
                reasonText = "PasswordExpired";
                return true;
            case "533":
                reasonText = "AccountDisabled";
                break;
            case "701":
                reasonText = "AccountExpired";
                break;
            case "773":
                reasonText = "UserMustChangePassword";
                return true;
            case "775":
                reasonText = "UserAccountLocked";
                break;
            default:
                reasonText = "UnknownError";
                break;
        }
        return false;
    }
}