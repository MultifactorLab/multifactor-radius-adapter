using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap.Attributes;
using Multifactor.Core.Ldap.Name;
using Multifactor.Radius.Adapter.v2.Application.Cache;
using Multifactor.Radius.Adapter.v2.Application.Core;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Ports;
using Multifactor.Radius.Adapter.v2.Application.Features.LoadProfile.Ports;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models.Enum;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps;

namespace Multifactor.Radius.Adapter.v2.Application.Features.LoadProfile;

public class ProfileLoadingStep : IRadiusPipelineStep
{
    private readonly IProfileSearch _profileSearch;
    private readonly ILogger<ProfileLoadingStep> _logger;
    private readonly ICacheService _cache;

    public ProfileLoadingStep(ILdapAdapter ldapAdapter, IProfileSearch profileSearch, ICacheService cache, ILogger<ProfileLoadingStep> logger)
    {
        _cache = cache;
        _profileSearch = profileSearch;
        _logger = logger;
    }

    public Task ExecuteAsync(RadiusPipelineContext context)
    {
        _logger.LogDebug("'{name}' started", nameof(ProfileLoadingStep));
        
        if (ShouldSkipStep(context))
            return Task.CompletedTask;
        
        ArgumentNullException.ThrowIfNull(context.LdapConfiguration);
        
        if (string.IsNullOrWhiteSpace(context.RequestPacket.UserName))
        {
            var clientAddress = context.RequestPacket.ProxyEndpoint?.Address.ToString() ?? context.RequestPacket.RemoteEndpoint.Address.ToString();
            _logger.LogWarning("No user name provided in packet '{id}' from '{client}'", context.RequestPacket.Identifier, clientAddress);
            return Task.CompletedTask;
        }

        if (context.LdapSchema is null)
        {
            _logger.LogError("No ldap schema loaded.");
            return Task.CompletedTask;
        }
        
        var userIdentity = new UserIdentity(context.RequestPacket.UserName);
        var attributes = GetAttributes(context).ToArray();
        // ОПРЕДЕЛЯЕМ, В КАКОМ ДОМЕНЕ ИСКАТЬ
        var searchBase = DetermineSearchBase(context, userIdentity);

        var profile = TryGetUserProfile(userIdentity, searchBase, attributes, context);
        
        if (profile is null)
        {
            _logger.LogWarning("Unable to load profile for user '{user}' from '{domain}'", userIdentity.Identity, searchBase.StringRepresentation);
            throw new InvalidOperationException();
        }
        
        context.LdapProfile = profile;
        _logger.LogInformation("Successfully found '{userIdentity}' profile at '{domain}'.", userIdentity.Identity, searchBase.StringRepresentation);
        
        return Task.CompletedTask;
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

    private ILdapProfile? TryGetUserProfile(UserIdentity userIdentity, DistinguishedName domain, LdapAttributeName[] attributes, RadiusPipelineContext context)
    {
        var cacheKey = $"{userIdentity.Identity}-{domain.StringRepresentation}";
        if (_cache.TryGetValue(cacheKey, out ILdapProfile? profile))
        {
            _logger.LogDebug("Loaded '{user}' profile from cache.", userIdentity.Identity);
            return profile;
        }
        var request = new FindUserRequest
        {
            ConnectionData = new LdapConnectionData
            {
                ConnectionString = context.LdapConfiguration.ConnectionString,
                UserName = context.LdapConfiguration.Username,
                Password = context.LdapConfiguration.Password,
                BindTimeoutInSeconds = context.LdapConfiguration.BindTimeoutSeconds
            },
            UserIdentity = userIdentity,
            SearchBase = domain,
            LdapSchema = context.LdapSchema,
            AttributeNames = attributes,
            Domain = domain,
        };
        profile = _profileSearch.Execute(request);

        if (profile is null)
            return profile;

        var expirationDate = DateTimeOffset.Now.AddHours(0); //TODO context.LdapConfiguration!.UserProfileCacheLifeTimeInHours = 0 ????
        SaveToCache(cacheKey, profile, expirationDate);
        
        _logger.LogDebug("'{userIdentity}' profile at '{domain}' is saved in cache till '{expirationDate}'.",  userIdentity.Identity, domain.StringRepresentation, expirationDate.ToString());
        
        return profile;
    }
    
    private static IList<LdapAttributeName> GetAttributes(RadiusPipelineContext context)
    {
        var attributes = new List<LdapAttributeName>() { new("memberOf"), new("userPrincipalName"), new("phone"), new("mail"), new("displayName"), new("email") };
        if (!string.IsNullOrWhiteSpace(context.LdapConfiguration!.IdentityAttribute))
            attributes.Add(new LdapAttributeName(context.LdapConfiguration.IdentityAttribute));

        var replyAttributes = context.ClientConfiguration.ReplyAttributes.Values
            .SelectMany(x => x)
            .Where(x => x.FromLdap)
            .Select(x => new LdapAttributeName(x.Name));
        
        attributes.AddRange(replyAttributes);
        attributes.AddRange(context.LdapConfiguration.PhoneAttributes.Select(x => new LdapAttributeName(x)));
        return attributes;
    }

    private void SaveToCache(string cacheKey, ILdapProfile profile, DateTimeOffset expirationDate)
    {
        _cache.Set(cacheKey, profile, expirationDate);
    }
    
    private bool ShouldSkipStep(RadiusPipelineContext context)
    {
        if (context.IsDomainAccount)
            return false;
        
        var packet = context.RequestPacket;
        
        _logger.LogInformation(
            "User '{user}' used '{accountType}' account to log in. Profile load is skipped.",
            packet.UserName,
            context.RequestPacket.AccountType);

        return true;
    }
}