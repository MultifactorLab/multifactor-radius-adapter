using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap.Attributes;
using Multifactor.Core.Ldap.Name;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.Ldap;
using Multifactor.Radius.Adapter.v2.Core.Ldap.Identity;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;
using Multifactor.Radius.Adapter.v2.Services.Ldap;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Steps;

public class ProfileLoadingStep : IRadiusPipelineStep
{
    private readonly ILdapProfileService _ldapProfileService;
    private readonly ILogger<ProfileLoadingStep> _logger;
    private readonly IMemoryCache _memoryCache;

    public ProfileLoadingStep(ILdapProfileService ldapProfileService, IMemoryCache memoryCache, ILogger<ProfileLoadingStep> logger)
    {
        _ldapProfileService = ldapProfileService;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public Task ExecuteAsync(IRadiusPipelineExecutionContext context)
    {
        _logger.LogDebug("'{name}' started", nameof(ProfileLoadingStep));
        if (string.IsNullOrWhiteSpace(context.RequestPacket.UserName))
        {
            var clientAddress = context.ProxyEndpoint?.Address.ToString() ?? context.RemoteEndpoint.Address.ToString();
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
        var domain = context.LdapSchema.NamingContext;
        
        var profile = TryGetUserProfile(userIdentity, domain, attributes, context);
        
        if (profile is null)
        {
            _logger.LogWarning("Unable to load profile for user '{user}' from '{domain}'", userIdentity.Identity, domain.StringRepresentation);
            return Task.CompletedTask;
        }
        
        context.UserLdapProfile = profile;
        _logger.LogInformation("Successfully found '{userIdentity}' profile at '{domain}'.", userIdentity.Identity, domain.StringRepresentation);
        
        return Task.CompletedTask;
    }

    private ILdapProfile? TryGetUserProfile(UserIdentity userIdentity, DistinguishedName domain, LdapAttributeName[] attributes, IRadiusPipelineExecutionContext context)
    {
        var cacheKey = $"{userIdentity.Identity}-{domain.StringRepresentation}";
        if (_memoryCache.TryGetValue(cacheKey, out ILdapProfile? profile))
        {
            _logger.LogDebug("Loaded '{user}' profile from cache.", userIdentity.Identity);
            return profile;
        }
                
        _logger.LogInformation("Try to find '{userIdentity}' profile at '{domain}'.", userIdentity.Identity, domain.StringRepresentation);
        profile = _ldapProfileService.FindUserProfile(new FindUserProfileRequest(context.ClientConfigurationName, context.LdapServerConfiguration, context.LdapSchema!, domain, userIdentity, attributes));

        if (profile is null)
            return profile;

        var expirationDate = DateTimeOffset.Now.AddHours(context.LdapServerConfiguration.UserProfileCacheLifeTimeInHours);
        SaveToCache(cacheKey, profile, expirationDate);
        
        _logger.LogDebug("'{userIdentity}' profile at '{domain}' is saved in cache till '{expirationDate}'.",  userIdentity.Identity, domain.StringRepresentation, expirationDate.ToString());
        
        return profile;
    }
    
    private IEnumerable<LdapAttributeName> GetAttributes(IRadiusPipelineExecutionContext context)
    {
        var attributes = new List<LdapAttributeName>() { new("memberOf"), new("userPrincipalName"), new("phone"), new("mail"), new("displayName"), new("email") };
        if (!string.IsNullOrWhiteSpace(context.LdapServerConfiguration.IdentityAttribute))
            attributes.Add(new LdapAttributeName(context.LdapServerConfiguration.IdentityAttribute));

        var replyAttributes = context.RadiusReplyAttributes.Values
            .SelectMany(x => x)
            .Where(x => x.FromLdap)
            .Select(x => new LdapAttributeName(x.LdapAttributeName));
        
        attributes.AddRange(replyAttributes);
        attributes.AddRange(context.LdapServerConfiguration.PhoneAttributes.Select(x => new LdapAttributeName(x)));
        return attributes;
    }

    private void SaveToCache(string cacheKey, ILdapProfile profile, DateTimeOffset expirationDate)
    {
        _memoryCache.Set(cacheKey, profile, expirationDate);
    }
}