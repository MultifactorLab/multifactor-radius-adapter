using System.Diagnostics;
using System.DirectoryServices.Protocols;
using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Attributes;
using Multifactor.Core.Ldap.Name;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Application.Core.Extensions;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Abstractions;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadLdapForest.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadProfile.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadProfile.Ports;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadProfile;

internal sealed class ProfileLoadingStep : IRadiusPipelineStep
{
    private readonly IProfileSearch _profileSearch;
    private readonly ILogger<ProfileLoadingStep> _logger;
    private const string StepName = nameof(ProfileLoadingStep); 
    
    private static readonly LdapAttributeName[] DefaultAttributes = 
    [
        new("memberOf"), 
        new("userPrincipalName"), 
        new("phone"), 
        new("mail"), 
        new("displayName"), 
        new("email")
    ];
    
    public ProfileLoadingStep(
        IProfileSearch profileSearch,
        ILogger<ProfileLoadingStep> logger)
    {
        _profileSearch = profileSearch;
        _logger = logger;
    }

    public async Task ExecuteAsync(RadiusPipelineContext context)
    {
        _logger.LogDebug("'{Name}' started", StepName);

        if (ShouldSkipStep(context))
            return;

        ValidateContext(context);

        var userIdentity = new UserIdentity(context.RequestPacket.UserName);
        var attributes = GetAttributes(context);
        var profile = await LoadUserProfileAsync(userIdentity, attributes, context);
        if (profile is null)
        {
            if (context.IsTerminated)
            {
                // Причина уже подробно залогирована внутри GC-ветки — не дублируем.
                return;
            }

            var searchBase = GetSearchBaseInfo(context);
            _logger.LogWarning(
                "Unable to load profile for user '{User}' from '{Domain}'", 
                userIdentity.Identity, 
                searchBase);

            if (context.LdapConfiguration!.IsGlobalCatalog())
            {
                // Пользователь не найден во всём лесу одним запросом к GC — отказ
                _logger.LogInformation(
                    "User '{User}' not found in Global Catalog. Rejecting immediately.",
                    userIdentity.Identity);
                context.FirstFactorStatus = AuthenticationStatus.Reject;
                context.SecondFactorStatus = AuthenticationStatus.Reject;
                context.Terminate();
                return;
            }

            throw new InvalidOperationException($"Failed to load profile for user {userIdentity.Identity}");
        }
        context.LdapProfile = profile;
        _logger.LogInformation(
            "Successfully found '{UserIdentity}' profile at '{Domain}'.", 
            userIdentity.Identity, 
            GetProfileLocation(profile, context));
    }

    private void ValidateContext(RadiusPipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.LdapConfiguration);

        if (!string.IsNullOrWhiteSpace(context.RequestPacket.UserName)) return;
        var clientAddress = context.RequestPacket.ProxyEndpoint?.Address.ToString() 
                            ?? context.RequestPacket.RemoteEndpoint?.Address.ToString();
        _logger.LogWarning(
            "No user name provided in packet '{PacketId}' from '{Client}'", 
            context.RequestPacket.Identifier, 
            clientAddress);
        throw new InvalidOperationException("Username is required");
    }

    private Task<ILdapProfile?> LoadUserProfileAsync(
        UserIdentity userIdentity, 
        List<LdapAttributeName> attributes, 
        RadiusPipelineContext context)
    {
        if (context.LdapConfiguration!.IsGlobalCatalog())
        {
            return Task.FromResult(LoadUserProfileFromGlobalCatalog(userIdentity, attributes, context));
        }

        var domainInfo = context.ForestMetadata?.DetermineForestDomain(userIdentity);
        
        if (domainInfo is not null)
        {
            return LoadProfileFromSpecificDomainAsync(userIdentity, attributes, context, domainInfo);
        }

        return Task.FromResult(TryGetUserProfile(userIdentity, attributes, context));
    }

    /// <summary>
    /// Один поисковый запрос ко всему лесу через Global Catalog.
    /// Из найденного DN вычисляется домен пользователя и connection-string для последующего
    /// bind напрямую к контроллеру этого домена (см. <see cref="RadiusPipelineContext.ResolvedBindConnectionString"/>).
    /// </summary>
    private ILdapProfile? LoadUserProfileFromGlobalCatalog(
        UserIdentity userIdentity,
        List<LdapAttributeName> attributes,
        RadiusPipelineContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var request = CreateFindUserRequest(
            context.LdapConfiguration!.ConnectionString,
            AuthType.Basic,
            userIdentity,
            DistinguishedName.Empty,
            context.LdapSchema!,
            attributes,
            context.LdapConfiguration);

        var matches = _profileSearch.ExecuteMany(request);
        stopwatch.Stop();

        _logger.LogInformation(
            "Global Catalog search for '{UserIdentity:l}' took {ElapsedMs} ms. Matches: {Count}",
            userIdentity.Identity, stopwatch.ElapsedMilliseconds, matches.Count);

        // Если пользователь явно указал домен (DOMAIN\user), независимо от того, сколько совпадений вернул GC (даже одно),
        // результат обязан принадлежать именно этому домену.
        if (userIdentity.Format == UserIdentityFormat.NetBiosName)
        {
            matches = FilterByNetBiosDomain(userIdentity, matches, context);
        }

        var profile = ResolveSingleMatch(userIdentity, matches, context);
        if (profile is null)
        {
            return null;
        }

        var globalCatalogConnectionString = new LdapConnectionString(context.LdapConfiguration.ConnectionString);
        var domainDnsName = profile.Dn.ExtractDomainDnsName();
        context.ResolvedBindConnectionString = globalCatalogConnectionString.ToDomainControllerConnectionString(domainDnsName);

        _logger.LogDebug(
            "User '{UserIdentity:l}' resolved to domain '{Domain:l}' via Global Catalog. Bind target: '{ConnectionString:l}'",
            userIdentity.Identity, domainDnsName, context.ResolvedBindConnectionString);

        var fullProfileStopwatch = Stopwatch.StartNew();
        var fullProfile = LoadFullProfileFromDomainController(userIdentity, attributes, context, profile.Dn);
        fullProfileStopwatch.Stop();

        if (fullProfile is null)
        {
            _logger.LogWarning(
                "Could not re-fetch full profile for '{UserIdentity:l}' from '{ConnectionString:l}' in {ElapsedMs} ms.",
                userIdentity.Identity, context.ResolvedBindConnectionString, fullProfileStopwatch.ElapsedMilliseconds);
            return profile;
        }

        _logger.LogInformation(
            "Re-fetched full profile for '{UserIdentity:l}' from '{ConnectionString:l}' in {ElapsedMs} ms (GC only returns a partial attribute set).",
            userIdentity.Identity, context.ResolvedBindConnectionString, fullProfileStopwatch.ElapsedMilliseconds);
        
        return fullProfile;
    }

    /// <summary>
    /// Перечитывает профиль напрямую с DC, которому принадлежит пользователь — по его точному DN.
    /// GC содержит только Partial Attribute Set, а MFA-группы, кастомные identity/phone/reply
    /// атрибуты должны читаться из полной реплики.
    /// </summary>
    private ILdapProfile? LoadFullProfileFromDomainController(
        UserIdentity userIdentity,
        List<LdapAttributeName> attributes,
        RadiusPipelineContext context,
        DistinguishedName userDn)
    {
        try
        {
            var request = CreateFindUserRequest(
                context.ResolvedBindConnectionString!,
                AuthType.Basic,
                userIdentity,
                userDn,
                context.LdapSchema!,
                attributes,
                context.LdapConfiguration!);

            return _profileSearch.Execute(request);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Error while re-fetching full profile for '{UserIdentity:l}' from '{ConnectionString:l}'.",
                userIdentity.Identity, context.ResolvedBindConnectionString);
            return null;
        }
    }

    /// <summary>
    /// Резолвит NetBIOS-домен из логина в DNS-имя и отфильтровывает GC-совпадения, оставляя
    /// только те, что принадлежат именно этому домену. Если домен не резолвится — считаем это
    /// поводом отказать, а не поводом искать без учёта домена.
    /// </summary>
    private IReadOnlyList<ILdapProfile> FilterByNetBiosDomain(
        UserIdentity userIdentity,
        IReadOnlyList<ILdapProfile> matches,
        RadiusPipelineContext context)
    {
        var index = userIdentity.Identity.IndexOf('\\');
        if (index <= 0)
        {
            return matches;
        }

        var netBiosName = userIdentity.Identity[..index];

        string? domainDns;
        try
        {
            domainDns = _profileSearch.ResolveDomainDnsNameByNetBiosName(
                context.LdapConfiguration!.ConnectionString,
                AuthType.Basic,
                context.LdapConfiguration.Username,
                context.LdapConfiguration.Password,
                context.LdapConfiguration.BindTimeoutSeconds,
                netBiosName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve NetBIOS domain '{NetBiosName:l}' to a DNS domain name.", netBiosName);
            domainDns = null;
        }

        if (string.IsNullOrWhiteSpace(domainDns))
        {
            _logger.LogWarning(
                "Could not resolve NetBIOS domain '{NetBiosName:l}' specified in login '{UserIdentity:l}'. " +
                "Treating as not found rather than searching without the domain the user explicitly specified.",
                netBiosName, userIdentity.Identity);
            return [];
        }

        var filtered = matches.Where(m => IsSameOrSubDomain(m.Dn.ExtractDomainDnsName(), domainDns)).ToList();

        if (filtered.Count != matches.Count)
        {
            _logger.LogInformation(
                "Filtered Global Catalog matches for '{UserIdentity:l}' by explicit NetBIOS domain '{NetBiosName:l}' ('{DomainDns:l}'): {Before} -> {After}.",
                userIdentity.Identity, netBiosName, domainDns, matches.Count, filtered.Count);
        }

        return filtered;
    }

    private static bool IsSameOrSubDomain(string domainDns, string expectedDomainDns) =>
        domainDns.Equals(expectedDomainDns, StringComparison.OrdinalIgnoreCase)
        || domainDns.EndsWith("." + expectedDomainDns, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Один и тот же логин может найтись сразу в нескольких доменах — sAMAccountName уникален
    /// только в пределах домена, не всего леса. Если так и есть, пробуем определить нужный домен
    /// по UPN. Не получилось — отказываем.
    /// </summary>
    private ILdapProfile? ResolveSingleMatch(UserIdentity userIdentity, IReadOnlyList<ILdapProfile> matches, RadiusPipelineContext context)
    {
        if (matches.Count == 0)
        {
            return null;
        }

        if (matches.Count == 1)
        {
            return matches[0];
        }

        var conflictingDns = matches.Select(m => m.Dn.StringRepresentation).ToList();
        _logger.LogWarning(
            "User '{UserIdentity:l}' matched {Count} entries in Global Catalog, login is not unique across the forest: {Dns}",
            userIdentity.Identity, matches.Count, conflictingDns);

        var match = TryFindMatchByUpnSuffix(userIdentity, matches);
        if (match is not null)
        {
            _logger.LogInformation(
                "Ambiguity for '{UserIdentity:l}' resolved by UPN suffix to '{Dn:l}'.",
                userIdentity.Identity, match.Dn.StringRepresentation);
            return match;
        }

        _logger.LogError(
            "User '{UserIdentity:l}' is ambiguous across {Count} domains and cannot be resolved automatically from the provided login. " +
            "Rejecting authentication instead of guessing. Conflicting DNs: {Dns}",
            userIdentity.Identity, matches.Count, conflictingDns);

        context.FirstFactorStatus = AuthenticationStatus.Reject;
        context.SecondFactorStatus = AuthenticationStatus.Reject;
        context.Terminate();
        return null;
    }

    private static ILdapProfile? TryFindMatchByUpnSuffix(UserIdentity userIdentity, IReadOnlyList<ILdapProfile> matches)
    {
        if (userIdentity.Format != UserIdentityFormat.UserPrincipalName)
        {
            return null;
        }

        var suffix = userIdentity.GetUpnSuffix();
        if (string.IsNullOrWhiteSpace(suffix))
        {
            return null;
        }

        var candidates = matches
            .Where(m =>
            {
                var domainDns = m.Dn.ExtractDomainDnsName();
                return domainDns.Equals(suffix, StringComparison.OrdinalIgnoreCase)
                    || domainDns.EndsWith("." + suffix, StringComparison.OrdinalIgnoreCase);
            })
            .ToList();

        return candidates.Count == 1 ? candidates[0] : null;
    }

    private Task<ILdapProfile?> LoadProfileFromSpecificDomainAsync(
        UserIdentity userIdentity,
        List<LdapAttributeName> attributes,
        RadiusPipelineContext context,
        DomainInfo domainInfo)
    {
        var searchBase = new DistinguishedName(domainInfo.DistinguishedName);
        var request = CreateFindUserRequest(
            domainInfo.ConnectionString,
            domainInfo?.GetAuthType() ?? AuthType.Basic,
            userIdentity,
            searchBase,
            domainInfo.Schema,
            attributes,
            context.LdapConfiguration);

        var profile = _profileSearch.Execute(request);
        
        if (profile is not null)
        {
            _logger.LogDebug("Found profile in specific domain '{Domain}'", searchBase.StringRepresentation);
        }

        return Task.FromResult(profile);
    }

    private static FindUserDto CreateFindUserRequest(
        string connectionString,
        AuthType authType,
        UserIdentity userIdentity,
        DistinguishedName searchBase,
        ILdapSchema schema,
        List<LdapAttributeName> attributes,
        ILdapServerConfiguration config)
    {
        var userName = config.Username;
        if (authType == AuthType.Negotiate)
        {
            userName = UserIdentity.TransformDnToUpn(config.Username);
        }
        return new FindUserDto
        {
            ConnectionString = connectionString,
            AuthType = authType,
            UserName = userName,
            Password = config.Password,
            BindTimeoutInSeconds = config.BindTimeoutSeconds,
            UserIdentity = userIdentity,
            SearchBase = searchBase,
            LdapSchema = schema,
            AttributeNames = attributes.ToArray()
        };
    }

    private ILdapProfile? TryGetUserProfile(
        UserIdentity userIdentity,
        List<LdapAttributeName> attributes,
        RadiusPipelineContext context)
    {
        var request = CreateFindUserRequest(
            context.LdapConfiguration!.ConnectionString,
            AuthType.Basic,
            userIdentity,
            context.LdapSchema.NamingContext,
            context.LdapSchema,
            attributes,
            context.LdapConfiguration);

        var profile = _profileSearch.Execute(request);

        if (profile is not null)
        {
            _logger.LogDebug(
                "'{UserIdentity}' profile at '{Domain}' was found.",
                userIdentity.Identity,
                context.LdapSchema.NamingContext.StringRepresentation);
        }

        return profile;
    }

    private static string GetProfileLocation(ILdapProfile profile, RadiusPipelineContext context)
    {
        if (!string.IsNullOrWhiteSpace(profile.Dn?.StringRepresentation)) 
            return profile.Dn.StringRepresentation;
        var userIdentity = new UserIdentity(context.RequestPacket.UserName);
        var schema = context.ForestMetadata?.DetermineForestDomain(userIdentity)?.Schema ?? context.LdapSchema;
        return schema?.NamingContext?.StringRepresentation
               ?? "unknown";
    }

    private static string GetSearchBaseInfo(RadiusPipelineContext context)
    {
        var userIdentity = new UserIdentity(context.RequestPacket.UserName);
        var schema = context.ForestMetadata?.DetermineForestDomain(userIdentity)?.Schema ?? context.LdapSchema;
        return schema?.NamingContext?.StringRepresentation
               ?? "unknown";
    }
    
    private static List<LdapAttributeName> GetAttributes(RadiusPipelineContext context)
    {
        var attributes = new List<LdapAttributeName>(DefaultAttributes);

        AddIfNotEmpty(attributes, context.LdapConfiguration!.IdentityAttribute);
        AddReplyAttributes(attributes, context.ClientConfiguration.ReplyAttributes);
        
        if(context.LdapConfiguration.PhoneAttributes?.Count > 0)
            attributes.AddRange(context.LdapConfiguration.PhoneAttributes
                                .Select(x => new LdapAttributeName(x)));
        return attributes;
    }

    private static void AddIfNotEmpty(List<LdapAttributeName> attributes, string? attributeName)
    {
        if (!string.IsNullOrWhiteSpace(attributeName))
        {
            attributes.Add(new LdapAttributeName(attributeName));
        }
    }

    private static void AddReplyAttributes(
        List<LdapAttributeName> attributes, 
        IReadOnlyDictionary<string, IReadOnlyList<IRadiusReplyAttribute>>? replyAttributes)
    {
        if (replyAttributes?.Values is null)
            return;

        foreach (var attr in replyAttributes.Values
                     .SelectMany(x => x)
                     .Where(x => x.FromLdap)
                     .Select(x => new LdapAttributeName(x.Name)))
        {
            attributes.Add(attr);
        }
    }

    private bool ShouldSkipStep(RadiusPipelineContext context)
    {
        if (context.IsDomainAccount)
            return false;

        _logger.LogInformation(
            "User '{User}' used '{AccountType}' account to log in. Profile load is skipped.",
            context.RequestPacket.UserName,
            context.RequestPacket.AccountType);
        return true;
    }
}