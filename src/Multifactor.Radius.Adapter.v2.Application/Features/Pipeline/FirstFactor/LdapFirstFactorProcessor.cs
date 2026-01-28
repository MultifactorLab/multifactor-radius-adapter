using System.DirectoryServices.Protocols;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Configuration.Models.Enum;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Ports;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.FirstFactor.BindNameFormat;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models.Enum;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.FirstFactor;

public class LdapFirstFactorProcessor : IFirstFactorProcessor
{
    private readonly ILdapBindNameFormatterProvider _ldapBindNameFormatterProvider;
    private readonly ILogger<LdapFirstFactorProcessor> _logger;
    private readonly ILdapAdapter  _ldapAdapter;

    public AuthenticationSource AuthenticationSource => AuthenticationSource.Ldap;

    public LdapFirstFactorProcessor(ILdapBindNameFormatterProvider ldapBindNameFormatterProvider, ILogger<LdapFirstFactorProcessor> logger, ILdapAdapter ldapAdapter)
    {;
        _logger = logger;
        _ldapAdapter = ldapAdapter;
        _ldapBindNameFormatterProvider = ldapBindNameFormatterProvider;
    }

    public Task ProcessFirstFactor(RadiusPipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        var radiusPacket = context.RequestPacket;

        if (context.LdapConfiguration is null)
            throw new InvalidOperationException("No Ldap servers configured.");

        if (string.IsNullOrWhiteSpace(radiusPacket.UserName))
        {
            _logger.LogWarning("Can't find User-Name in message id={id} from {host:l}:{port}", radiusPacket.Identifier, context.RequestPacket.RemoteEndpoint.Address, context.RequestPacket.RemoteEndpoint.Port);
            Reject(context);
            return Task.CompletedTask;
        }

        var transformedName = radiusPacket.UserName;

        var passphrase = context.Passphrase;
        if (string.IsNullOrWhiteSpace(passphrase.Raw))
        {
            _logger.LogWarning("No User-Password in message id={id} from {host:l}:{port}", radiusPacket.Identifier, context.RequestPacket.RemoteEndpoint.Address, context.RequestPacket.RemoteEndpoint.Port);
            Reject(context);
            return Task.CompletedTask;
        }
        
        if (string.IsNullOrWhiteSpace(passphrase.Password))
        {
            _logger.LogWarning("Can't parse User-Password in message id={id} from {host:l}:{port}", radiusPacket.Identifier, context.RequestPacket.RemoteEndpoint.Address, context.RequestPacket.RemoteEndpoint.Port);
            Reject(context);
            return Task.CompletedTask;
        }

        var isValid = ValidateUserCredentials(context, transformedName, passphrase.Password);
        if (!isValid)
        {
            Reject(context);
            return Task.CompletedTask;
        }

        _logger.LogInformation("User '{user:l}' credential and status verified successfully at {endpoint:l}", transformedName, context.LdapConfiguration.ConnectionString);
        Accept(context);
        return Task.CompletedTask;
    }

    private bool ValidateUserCredentials(
        RadiusPipelineContext context,
        string login,
        string password)
    {
        var serverConfig = context.LdapConfiguration;
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
            if (context.LdapProfile is not null)
                formatted = formatter?.FormatName(login, context.LdapProfile);
            
            bindName = string.IsNullOrWhiteSpace(formatted) ? login : formatted;
            
            _logger.LogDebug("Use '{name}' for LDAP bind.", bindName);
            var request = new LdapConnectionData
            {
                ConnectionString = serverConfig.ConnectionString,
                UserName = bindName,
                Password = password,
                BindTimeoutInSeconds = serverConfig.BindTimeoutSeconds
            };

            return _ldapAdapter.CheckConnection(request);
        }
        catch (Exception ex)
        {
            if (ex is not LdapException ldapException)
            {
                _logger.LogError(ex, "Verification user '{user:l}' at {ldapUri:l} failed", bindName, serverConfig.ConnectionString);
                return false;
            }
            if(CheckLdapException(ldapException, out var reasonText))
                context.MustChangePasswordDomain = context.LdapConfiguration.ConnectionString;

            _logger.LogWarning(ldapException, "Verification user '{user:l}' at {ldapUri:l} failed: {dataReason:l}", bindName, serverConfig.ConnectionString, reasonText);
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
                case "525": reasonText = "UserNotFound";
                    break;
                case "52e": reasonText = "InvalidCredentials";
                    break;
                case "530": reasonText = "NotPermittedToLogonAtThisTime";
                    break;
                case "531": reasonText = "NotPermittedToLogonAtThisWorkstation";
                    break;
                case "532": reasonText = "PasswordExpired";
                    return true;
                case "533": reasonText = "AccountDisabled";
                    break;
                case "701": reasonText = "AccountExpired";
                    break;
                case "773": reasonText = "UserMustChangePassword";
                    return true;
                case "775": reasonText = "UserAccountLocked";
                    break;
                default: reasonText = "UnknownError";
                    break;
            }
            return false;
    }
}