using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Application.Core;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Enum;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Ports;
using Multifactor.Radius.Adapter.v2.Application.Features.LoadLdapForest.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.FirstFactor;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.FirstFactor.BindNameFormat;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models.Enum;
using System.DirectoryServices.Protocols;
using System.Text.RegularExpressions;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.FirstFactor;

public class LdapFirstFactorProcessor : IFirstFactorProcessor
{
    private readonly ILdapBindNameFormatterProvider _ldapBindNameFormatterProvider;
    private readonly ILogger<LdapFirstFactorProcessor> _logger;
    private readonly ILdapAdapter _ldapAdapter;

    public AuthenticationSource AuthenticationSource => AuthenticationSource.Ldap;

    public LdapFirstFactorProcessor(
        ILdapBindNameFormatterProvider ldapBindNameFormatterProvider,
        ILogger<LdapFirstFactorProcessor> logger,
        ILdapAdapter ldapAdapter)
    {
        _logger = logger;
        _ldapAdapter = ldapAdapter;
        _ldapBindNameFormatterProvider = ldapBindNameFormatterProvider;
    }

    public async Task ProcessFirstFactor(RadiusPipelineContext context) // ← async
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        var radiusPacket = context.RequestPacket;

        if (context.LdapConfiguration is null)
            throw new InvalidOperationException("No Ldap servers configured.");

        if (string.IsNullOrWhiteSpace(radiusPacket.UserName))
        {
            _logger.LogWarning("Can't find User-Name in message id={id} from {host:l}:{port}",
                radiusPacket.Identifier, context.RequestPacket.RemoteEndpoint.Address, context.RequestPacket.RemoteEndpoint.Port);
            Reject(context);
            return;
        }

        var passphrase = context.Passphrase;
        if (string.IsNullOrWhiteSpace(passphrase.Raw))
        {
            _logger.LogWarning("No User-Password in message id={id} from {host:l}:{port}",
                radiusPacket.Identifier, context.RequestPacket.RemoteEndpoint.Address, context.RequestPacket.RemoteEndpoint.Port);
            Reject(context);
            return;
        }

        if (string.IsNullOrWhiteSpace(passphrase.Password))
        {
            _logger.LogWarning("Can't parse User-Password in message id={id} from {host:l}:{port}",
                radiusPacket.Identifier, context.RequestPacket.RemoteEndpoint.Address, context.RequestPacket.RemoteEndpoint.Port);
            Reject(context);
            return;
        }

        // ПОЛУЧАЕМ ВСЕ ВОЗМОЖНЫЕ ВАРИАНТЫ ДЛЯ АУТЕНТИФИКАЦИИ
        var authVariants = GetAuthenticationVariants(context);

        bool isValid = false;
        string? successfulBindName = null;

        // Пробуем каждый вариант
        foreach (var variant in authVariants)
        {
            _logger.LogDebug("Trying authentication with bind name '{bindName}' to domain '{domain}'",
                variant.BindName, variant.Domain?.DnsName ?? "default");

            isValid = ValidateUserCredentials(context, variant.BindName, passphrase.Password, variant.ConnectionString);

            if (isValid)
            {
                successfulBindName = variant.BindName;
                _logger.LogInformation("User authenticated successfully with bind name '{bindName}'", variant.BindName);

                break;
            }
        }

        if (!isValid)
        {
            _logger.LogWarning("All authentication attempts failed for user '{user}'", radiusPacket.UserName);
            Reject(context);
            return;
        }

        _logger.LogInformation("User '{user:l}' credential verified successfully", radiusPacket.UserName);
        Accept(context);
    }

    /// <summary>
    /// ПОЛУЧАЕТ ВСЕ ВАРИАНТЫ ДЛЯ АУТЕНТИФИКАЦИИ (С УЧЕТОМ ЛЕСА)
    /// </summary>
    private List<AuthenticationVariant> GetAuthenticationVariants(RadiusPipelineContext context)
    {
        var variants = new List<AuthenticationVariant>();
        var userIdentity = new UserIdentity(context.RequestPacket.UserName);

        // ВАРИАНТ 1: Сначала пробуем с original именем (как пришло)
        variants.Add(new AuthenticationVariant
        {
            BindName = context.RequestPacket.UserName,
            ConnectionString = context.LdapConfiguration!.ConnectionString,
            Domain = null
        });

        // Если нет метаданных леса - только оригинальный вариант
        if (context.ForestMetadata == null)
            return variants;

        // ВАРИАНТ 2: Для NetBIOS пробуем преобразовать в UPN
        if (userIdentity.Format == UserIdentityFormat.NetBiosName)
        {
            var domain = context.ForestMetadata.GetDomainByNetBios(userIdentity.Identity);
            if (domain != null)
            {
                var upnName = $"{userIdentity.Identity}@{domain.DnsName}";
                variants.Add(new AuthenticationVariant
                {
                    BindName = upnName,
                    ConnectionString = context.LdapConfiguration!.ConnectionString,
                    Domain = domain
                });

                _logger.LogDebug("Added NetBIOS conversion variant: {original} -> {upn}",
                    context.RequestPacket.UserName, upnName);
            }
        }

        // ВАРИАНТ 3: Для UPN пробуем разные суффиксы (если есть доверенные домены)
        if (userIdentity.Format == UserIdentityFormat.UserPrincipalName)
        {
            var suffix = userIdentity.GetUpnSuffix();

            // Ищем все домены, которые могут обработать этот суффикс
            var domains = context.ForestMetadata.GetDomainsByUpnSuffix(suffix);

            foreach (var domain in domains)
            {
                // Пропускаем текущий домен (уже есть в варианте 1)
                if (domain.DnsName == suffix)
                    continue;

                variants.Add(new AuthenticationVariant
                {
                    BindName = context.RequestPacket.UserName, // То же имя
                    ConnectionString = context.LdapConfiguration!.ConnectionString,
                    Domain = domain
                });

                _logger.LogDebug("Added alternative domain variant: {user} -> {domain}",
                    context.RequestPacket.UserName, domain.DnsName);
            }
        }

        // ВАРИАНТ 4: Если есть профиль, пробуем форматированное имя
        if (context.LdapProfile != null)
        {
            var ldapImpl = context.LdapSchema?.LdapServerImplementation ?? LdapImplementation.Unknown;
            var formatter = _ldapBindNameFormatterProvider.GetLdapBindNameFormatter(ldapImpl);

            if (formatter != null)
            {
                var formatted = formatter.FormatName(context.RequestPacket.UserName, context.LdapProfile);
                if (!string.IsNullOrWhiteSpace(formatted) &&
                    formatted != context.RequestPacket.UserName)
                {
                    variants.Add(new AuthenticationVariant
                    {
                        BindName = formatted,
                        ConnectionString = context.LdapConfiguration!.ConnectionString
                    });

                    _logger.LogDebug("Added formatted bind name variant: {original} -> {formatted}",
                        context.RequestPacket.UserName, formatted);
                }
            }
        }

        return variants;
    }

    private bool ValidateUserCredentials(
        RadiusPipelineContext context,
        string login,
        string password,
        string connectionString) // ДОБАВИЛИ connectionString
    {
        var serverConfig = context.LdapConfiguration;
        if (serverConfig is null)
            throw new InvalidOperationException("No Ldap servers configured.");

        var bindName = string.Empty;

        try
        {
            bindName = login;

            _logger.LogDebug("Use '{name}' for LDAP bind.", bindName);
            var request = new LdapConnectionData
            {
                ConnectionString = connectionString, // Используем переданную строку
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
                _logger.LogError(ex, "Verification user '{user:l}' at {ldapUri:l} failed",
                    bindName, connectionString);
                return false;
            }

            if (CheckLdapException(ldapException, out var reasonText))
                context.MustChangePasswordDomain = connectionString;

            _logger.LogWarning(ldapException, "Verification user '{user:l}' at {ldapUri:l} failed: {dataReason:l}",
                bindName, connectionString, reasonText);
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

    /// <summary>
    /// КЛАСС ДЛЯ ВАРИАНТОВ АУТЕНТИФИКАЦИИ
    /// </summary>
    private class AuthenticationVariant
    {
        public string BindName { get; set; }
        public string ConnectionString { get; set; }
        public DomainInfo? Domain { get; set; }
    }
}