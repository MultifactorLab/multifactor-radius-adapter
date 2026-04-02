using System.DirectoryServices.Protocols;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Name;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Abstractions;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadLdapForest.Models;

namespace Multifactor.Radius.Adapter.v2.Application.SharedServices;

/// <summary>
/// Сервис для работы с LDAP лесом - поиск доменов, построение connection string, обработка ошибок
/// </summary>
public interface ILdapForestService
{
    /// <summary>
    /// Определяет поисковую базу (DN) на основе метаданных леса и формата имени пользователя
    /// </summary>
    DistinguishedName? DetermineSearchBase(
        IForestMetadata? forestMetadata,
        UserIdentity userIdentity,
        ILdapSchema? fallbackSchema = null);

    /// <summary>
    /// Строит connection string для конкретного домена на основе базового
    /// </summary>
    LdapConnectionString BuildConnectionStringForDomain(
        string baseConnectionString,
        DistinguishedName domain);

    /// <summary>
    /// Получает все возможные варианты для аутентификации пользователя
    /// </summary>
    List<AuthenticationVariant> GetAuthenticationVariants(
        string originalUserName,
        IForestMetadata? forestMetadata,
        LdapConnectionString baseConnectionString,
        ILdapProfile? ldapProfile = null);

    /// <summary>
    /// Парсит LDAP исключение и определяет причину ошибки
    /// </summary>
    LdapExceptionInfo ParseLdapException(LdapException exception);

    /// <summary>
    /// Проверяет, нужно ли менять пароль (password expired или must change)
    /// </summary>
    bool IsPasswordChangeRequired(LdapException exception, out string reasonText);

    /// <summary>
    /// Конвертирует Distinguished Name в FQDN
    /// </summary>
    string DnToFqdn(DistinguishedName name);
    

    /// <summary>
    /// Получает все домены, которые могут обработать UPN суффикс
    /// </summary>
    IReadOnlyList<DomainInfo> GetDomainsByUpnSuffix(IForestMetadata forestMetadata, string suffix);

    /// <summary>
    /// Получает домен по NetBIOS имени
    /// </summary>
    DomainInfo? GetDomainByNetBios(IForestMetadata forestMetadata, string netBiosName);

    /// <summary>
    /// Получает корневой домен леса
    /// </summary>
    DomainInfo? GetRootDomain(IForestMetadata forestMetadata);
}

/// <summary>
/// Вариант аутентификации
/// </summary>
public sealed record AuthenticationVariant(
    string BindName,
    string ConnectionString,
    DomainInfo? Domain = null);

/// <summary>
/// Информация об LDAP исключении
/// </summary>
public sealed record LdapExceptionInfo(
    bool RequiresPasswordChange,
    string ReasonText,
    string ErrorCode,
    bool IsLdapError);

public sealed class LdapForestService : ILdapForestService
{
    private readonly ILogger<LdapForestService> _logger;

    // Маппинг кодов ошибок LDAP
    private static readonly IReadOnlyDictionary<string, LdapErrorMapping> ErrorMappings = 
        new Dictionary<string, LdapErrorMapping>
        {
            ["525"] = new("UserNotFound", "User not found", false),
            ["52e"] = new("InvalidCredentials", "Invalid credentials", false),
            ["530"] = new("NotPermittedToLogonAtThisTime", "Not permitted to logon at this time", false),
            ["531"] = new("NotPermittedToLogonAtThisWorkstation", "Not permitted to logon at this workstation", false),
            ["532"] = new("PasswordExpired", "Password expired", true),
            ["533"] = new("AccountDisabled", "Account disabled", false),
            ["701"] = new("AccountExpired", "Account expired", false),
            ["773"] = new("UserMustChangePassword", "User must change password", true),
            ["775"] = new("UserAccountLocked", "User account locked", false),
        };

    public LdapForestService(ILogger<LdapForestService> logger)
    {
        _logger = logger;
    }

    public DistinguishedName? DetermineSearchBase(
        IForestMetadata? forestMetadata,
        UserIdentity userIdentity,
        ILdapSchema? fallbackSchema = null)
    {
        // Нет метаданных - используем fallback
        if (forestMetadata == null)
        {
            _logger.LogDebug("No forest metadata, using fallback schema naming context");
            return fallbackSchema?.NamingContext;
        }

        var result = userIdentity.Format switch
        {
            UserIdentityFormat.UserPrincipalName => FindDomainByUpnSuffixInternal(forestMetadata, userIdentity.GetUpnSuffix()),
            UserIdentityFormat.NetBiosName => GetDomainByNetBios(forestMetadata, userIdentity.Identity),
            UserIdentityFormat.SamAccountName => GetRootDomain(forestMetadata),
            _ => null
        };

        if (result != null)
        {
            _logger.LogDebug("Determined search base '{Dn}' for user '{User}' with format '{Format}'",
                result.DistinguishedName, userIdentity.Identity, userIdentity.Format);
            return new DistinguishedName(result.DistinguishedName);
        }

        // Fallback если не нашли
        _logger.LogDebug("Could not determine search base from forest metadata, using fallback");
        return fallbackSchema?.NamingContext;
    }

    public LdapConnectionString BuildConnectionStringForDomain(string baseConnectionString, DistinguishedName domain)
    {
        var parsedBase = new LdapConnectionString(baseConnectionString, true);
        var fqdn = DnToFqdn(domain);
        
        var result = new LdapConnectionString($"{parsedBase.Scheme}://{fqdn}:{parsedBase.Port}", true);
        
        _logger.LogDebug("Built connection string for domain '{Domain}': {ConnectionString}",
            fqdn, result.ToString());
        
        return result;
    }

    public List<AuthenticationVariant> GetAuthenticationVariants(
        string originalUserName,
        IForestMetadata? forestMetadata,
        LdapConnectionString baseConnectionString,
        ILdapProfile? ldapProfile = null)
    {
        var variants = new List<AuthenticationVariant>();
        var userIdentity = new UserIdentity(originalUserName);
        var connectionString = baseConnectionString.ToString();

        // ВАРИАНТ 1: Оригинальное имя
        variants.Add(new AuthenticationVariant(originalUserName, connectionString));

        // Нет метаданных - только оригинал
        if (forestMetadata == null)
            return variants;

        // ВАРИАНТ 2: NetBIOS -> UPN
        if (userIdentity.Format == UserIdentityFormat.NetBiosName)
        {
            var domain = GetDomainByNetBios(forestMetadata, userIdentity.Identity);
            if (domain != null)
            {
                var upnName = $"{userIdentity.Identity}@{domain.DnsName}";
                variants.Add(new AuthenticationVariant(upnName, connectionString, domain));
                _logger.LogDebug("Added NetBIOS->UPN variant: {Original} -> {Upn}", originalUserName, upnName);
            }
        }

        // ВАРИАНТ 3: UPN -> другие домены с таким же суффиксом
        if (userIdentity.Format == UserIdentityFormat.UserPrincipalName)
        {
            var suffix = userIdentity.GetUpnSuffix();
            var domains = GetDomainsByUpnSuffix(forestMetadata, suffix);

            foreach (var domain in domains)
            {
                if (domain.DnsName != suffix) // пропускаем текущий
                {
                    variants.Add(new AuthenticationVariant(originalUserName, connectionString, domain));
                    _logger.LogDebug("Added alternative domain variant: {User} -> {Domain}",
                        originalUserName, domain.DnsName);
                }
            }
        }

        // ВАРИАНТ 4: Форматированное имя из профиля
        if (ldapProfile.DisplayName != null)
        {
            var formatted = FormatBindName(originalUserName, ldapProfile.DisplayName);
            if (!string.IsNullOrWhiteSpace(formatted) && formatted != originalUserName)
            {
                variants.Add(new AuthenticationVariant(formatted, connectionString));
                _logger.LogDebug("Added formatted bind name variant: {Original} -> {Formatted}",
                    originalUserName, formatted);
            }
        }

        return variants;
    }

    public LdapExceptionInfo ParseLdapException(LdapException exception)
    {
        if (string.IsNullOrWhiteSpace(exception.ServerErrorMessage))
        {
            return new LdapExceptionInfo(false, "UnknownError", "000", true);
        }

        var pattern = @"data ([0-9a-e]{3})";
        var match = Regex.Match(exception.ServerErrorMessage, pattern);

        if (!match.Success || match.Groups.Count != 2)
        {
            return new LdapExceptionInfo(false, "UnknownError", "000", true);
        }

        var errorCode = match.Groups[1].Value;
        
        if (ErrorMappings.TryGetValue(errorCode, out var mapping))
        {
            return new LdapExceptionInfo(
                mapping.RequiresPasswordChange,
                mapping.ReasonText,
                errorCode,
                true);
        }

        return new LdapExceptionInfo(false, "UnknownError", errorCode, true);
    }

    public bool IsPasswordChangeRequired(LdapException exception, out string reasonText)
    {
        var info = ParseLdapException(exception);
        reasonText = info.ReasonText;
        return info.RequiresPasswordChange;
    }

    public string DnToFqdn(DistinguishedName name)
    {
        return string.Join(".", name.Components.Reverse().Select(x => x.Value));
    }

    public IReadOnlyList<DomainInfo> GetDomainsByUpnSuffix(IForestMetadata forestMetadata, string suffix)
    {
        var domains = new List<DomainInfo>();

        // Точное совпадение
        if (forestMetadata.UpnSuffixes.TryGetValue(suffix, out var exactDomain))
        {
            domains.Add(exactDomain);
        }

        // Частичные совпадения (для дочерних доменов)
        foreach (var kv in forestMetadata.UpnSuffixes)
        {
            if (suffix.EndsWith($".{kv.Key}") || suffix == kv.Key)
            {
                if (!domains.Contains(kv.Value))
                    domains.Add(kv.Value);
            }
        }

        _logger.LogDebug("Found {Count} domains for UPN suffix '{Suffix}'", domains.Count, suffix);
        return domains;
    }

    public DomainInfo? GetDomainByNetBios(IForestMetadata forestMetadata, string netBiosName)
    {
        if (forestMetadata.NetBiosNames.TryGetValue(netBiosName, out var domain))
        {
            _logger.LogDebug("Found domain '{Domain}' for NetBIOS name '{NetBios}'",
                domain.DnsName, netBiosName);
            return domain;
        }

        _logger.LogDebug("No domain found for NetBIOS name '{NetBios}'", netBiosName);
        return null;
    }

    public DomainInfo? GetRootDomain(IForestMetadata forestMetadata)
    {
        var rootDomain = forestMetadata.Domains.Values
            .FirstOrDefault(d => d.DnsName == forestMetadata.RootDomain);

        if (rootDomain != null)
        {
            _logger.LogDebug("Using root domain '{Domain}'", rootDomain.DnsName);
        }

        return rootDomain;
    }

    private DomainInfo? FindDomainByUpnSuffixInternal(IForestMetadata forestMetadata, string suffix)
    {
        // Точное совпадение
        if (forestMetadata.UpnSuffixes.TryGetValue(suffix, out var domain))
        {
            return domain;
        }

        // Частичное совпадение
        foreach (var kv in forestMetadata.UpnSuffixes)
        {
            if (suffix.EndsWith(kv.Key))
            {
                _logger.LogDebug("Found partial match: domain '{Domain}' for suffix '{Suffix}'",
                    kv.Value.DnsName, suffix);
                return kv.Value;
            }
        }

        return null;
    }

    private string? FormatBindName(string userName, string format)
    {
        // Пример: можно расширить под разные форматы
        if (string.IsNullOrWhiteSpace(format))
            return userName;

        return format.Replace("{username}", userName);
    }

    private sealed record LdapErrorMapping(string ReasonText, string Description, bool RequiresPasswordChange);
}