using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap.Attributes;
using Multifactor.Core.Ldap.Name;
using Multifactor.Radius.Adapter.v2.Application.Cache;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Ports;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps;

public class ProfileLoadingStep : IRadiusPipelineStep
{
    private readonly ILdapAdapter _ldapAdapter;
    private readonly ILogger<ProfileLoadingStep> _logger;
    private readonly ICacheService _cache;

    public ProfileLoadingStep(ILdapAdapter ldapAdapter, ICacheService cache, ILogger<ProfileLoadingStep> logger)
    {
        _ldapAdapter = ldapAdapter;
        _cache = cache;
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
        var domain = context.LdapSchema.NamingContext;
        
        var profile = TryGetUserProfile(userIdentity, domain, attributes, context);
        
        if (profile is null)
        {
            _logger.LogWarning("Unable to load profile for user '{user}' from '{domain}'", userIdentity.Identity, domain.StringRepresentation);
            throw new InvalidOperationException();
        }
        
        context.LdapProfile = profile;
        _logger.LogInformation("Successfully found '{userIdentity}' profile at '{domain}'.", userIdentity.Identity, domain.StringRepresentation);
        
        return Task.CompletedTask;
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
        };
        profile = _ldapAdapter.FindUserProfile(request);

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