using System.DirectoryServices.Protocols;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Name;
using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.FirstFactor.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.FirstFactor.Ports;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadLdapForest.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.FirstFactor.Processor;

internal sealed class LdapFirstFactorProcessor : IFirstFactorProcessor
{
    private readonly ILogger<LdapFirstFactorProcessor> _logger; //todo
    private readonly ICheckConnection _checkConnection;
    public AuthenticationSource AuthenticationSource => AuthenticationSource.Ldap;

    public LdapFirstFactorProcessor(
        ILogger<LdapFirstFactorProcessor> logger,
        ICheckConnection checkConnection)
    {
        _logger = logger;
        _checkConnection = checkConnection;
    }

    public Task Execute(RadiusPipelineContext context) 
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
            return Task.CompletedTask;
        }

        var passphrase = context.Passphrase;
        if (string.IsNullOrWhiteSpace(passphrase.Raw))
        {
            _logger.LogWarning("No User-Password in message id={id} from {host:l}:{port}",
                radiusPacket.Identifier, context.RequestPacket.RemoteEndpoint.Address, context.RequestPacket.RemoteEndpoint.Port);
            Reject(context);
            return Task.CompletedTask;
        }

        if (string.IsNullOrWhiteSpace(passphrase.Password))
        {
            _logger.LogWarning("Can't parse User-Password in message id={id} from {host:l}:{port}",
                radiusPacket.Identifier, context.RequestPacket.RemoteEndpoint.Address, context.RequestPacket.RemoteEndpoint.Port);
            Reject(context);
            return Task.CompletedTask;
        }

        // ПОЛУЧАЕМ ВСЕ ВОЗМОЖНЫЕ ВАРИАНТЫ ДЛЯ АУТЕНТИФИКАЦИИ
        var authVariants = GetAuthenticationVariants(context);

        bool isValid = false;

        // Пробуем каждый вариант
        foreach (var variant in authVariants)
        {
            _logger.LogDebug("Trying authentication with bind name '{bindName}' to domain '{domain}'",
                variant.BindName, variant.Domain?.DnsName ?? "default");

            isValid = ValidateUserCredentials(context, variant.BindName, passphrase.Password, variant.ConnectionString);

            if (isValid)
            {
                _logger.LogInformation("User authenticated successfully with bind name '{bindName}'", variant.BindName);
                break;
            }
        }

        if (!isValid)
        {
            _logger.LogWarning("All authentication attempts failed for user '{user}'", radiusPacket.UserName);
            Reject(context);
            return Task.CompletedTask;
        }

        _logger.LogInformation("User '{user:l}' credential verified successfully", radiusPacket.UserName);
        Accept(context);
        return Task.CompletedTask;
    }

    /// <summary>
    /// ПОЛУЧАЕТ ВСЕ ВАРИАНТЫ ДЛЯ АУТЕНТИФИКАЦИИ (С УЧЕТОМ ЛЕСА)
    /// </summary>
    private List<AuthenticationVariant> GetAuthenticationVariants(RadiusPipelineContext context)
    {
        var variants = new List<AuthenticationVariant>();
        var userIdentity = new UserIdentity(context.RequestPacket.UserName);

        var domain2 = DetermineSearchBase(context, userIdentity);
        var connectionString = GetDomainConnectionString(new LdapConnectionString(context.LdapConfiguration!.ConnectionString), domain2);

        // ВАРИАНТ 1: Сначала пробуем с original именем (как пришло)
        variants.Add(new AuthenticationVariant(context.RequestPacket.UserName, connectionString));

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
                variants.Add(new AuthenticationVariant(upnName, connectionString, domain));

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

                variants.Add(new AuthenticationVariant(context.RequestPacket.UserName, connectionString, domain));

                _logger.LogDebug("Added alternative domain variant: {user} -> {domain}",
                    context.RequestPacket.UserName, domain.DnsName);
            }
        }

        // ВАРИАНТ 4: Если есть профиль, пробуем форматированное имя
        if (context.LdapProfile == null) return variants;
        var formatted = LdapBindNameFormatter.FormatName(context.RequestPacket.UserName, context.LdapProfile);
        if (string.IsNullOrWhiteSpace(formatted) ||
            formatted == context.RequestPacket.UserName) return variants;
        variants.Add(new AuthenticationVariant(formatted, connectionString));

        _logger.LogDebug("Added formatted bind name variant: {original} -> {formatted}",
            context.RequestPacket.UserName, formatted);

        return variants;
    }

    private bool ValidateUserCredentials(
        RadiusPipelineContext context,
        string login,
        string password,
        string connectionString)
    {
        var serverConfig = context.LdapConfiguration;
        if (serverConfig is null)
            throw new InvalidOperationException("No Ldap servers configured.");

        var bindName = login;

        try
        {
            _logger.LogDebug("Use '{name}' for LDAP bind.", bindName);

            var request = new CheckConnectionDto(connectionString, bindName,
                password, serverConfig.BindTimeoutSeconds);

            return _checkConnection.Execute(request);
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
    private static string GetDomainConnectionString(LdapConnectionString ldapConnectionString, DistinguishedName name)
    {
        var ncs = name.Components.Reverse();
        var newHost = string.Join(".", ncs.Select(x => x.Value));
        var initialLdapSchema = ldapConnectionString.Scheme;
        var initialLdapPort = ldapConnectionString.Port;
        return $"{initialLdapSchema}://{newHost}:{initialLdapPort}";
    }

    private static void Reject(RadiusPipelineContext context)
    {
        context.FirstFactorStatus = AuthenticationStatus.Reject;
    }

    private static void Accept(RadiusPipelineContext context)
    {
        context.FirstFactorStatus = AuthenticationStatus.Accept;
    }

    private DistinguishedName DetermineSearchBase(RadiusPipelineContext context, UserIdentity userIdentity)
    {
        // Если есть метаданные леса - используем их
        if (context.ForestMetadata != null)
        {
            // Для UPN - ищем по суффиксу
            if (userIdentity.Format == UserIdentityFormat.UserPrincipalName)
            {
                var suffix = userIdentity.GetUpnSuffix();
                if (context.ForestMetadata.UpnSuffixes.TryGetValue(suffix, out var domain))
                {
                    _logger.LogDebug("Found domain '{domain}' for UPN suffix '{suffix}'",
                        domain.DnsName, suffix);
                    return new DistinguishedName(domain.DistinguishedName);
                }

                // Частичное совпадение (для дочерних доменов)
                foreach (var kv in context.ForestMetadata.UpnSuffixes)
                {
                    if (suffix.EndsWith(kv.Key))
                    {
                        _logger.LogDebug("Found partial match: domain '{domain}' for suffix '{suffix}'",
                            kv.Value.DnsName, suffix);
                        return new DistinguishedName(kv.Value.DistinguishedName);
                    }
                }
            }

            // Для NetBIOS - ищем по NetBIOS имени
            if (userIdentity.Format == UserIdentityFormat.NetBiosName &&
                context.ForestMetadata.NetBiosNames.TryGetValue(userIdentity.Identity, out var netbiosDomain))
            {
                _logger.LogDebug("Found domain '{domain}' for NetBIOS name '{netbios}'",
                    netbiosDomain.DnsName, userIdentity.Identity);
                return new DistinguishedName(netbiosDomain.DistinguishedName);
            }

            // Для SAM Account Name без домена - используем корневой домен
            if (userIdentity.Format == UserIdentityFormat.SamAccountName)
            {
                var rootDomain = context.ForestMetadata.Domains.Values
                    .FirstOrDefault(d => d.DnsName == context.ForestMetadata.RootDomain);

                if (rootDomain != null)
                {
                    _logger.LogDebug("Using root domain '{domain}' for SAM account name",
                        rootDomain.DnsName);
                    return new DistinguishedName(rootDomain.DistinguishedName);
                }
            }
        }

        // Fallback - используем naming context из схемы
        _logger.LogDebug("Using schema naming context as fallback");
        return context.LdapSchema.NamingContext;
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
    
    private sealed record AuthenticationVariant(string BindName, string ConnectionString, DomainInfo? Domain = null);
}