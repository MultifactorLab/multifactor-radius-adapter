using System.DirectoryServices.Protocols;
using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Attributes;
using Multifactor.Core.Ldap.Name;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Abstractions;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Dto;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadLdapForest.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadProfile.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadProfile.Ports;
using Multifactor.Radius.Adapter.v2.Application.SharedPorts;

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
            var searchBase = GetSearchBaseInfo(context);
            _logger.LogWarning(
                "Unable to load profile for user '{User}' from '{Domain}'", 
                userIdentity.Identity, 
                searchBase);
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

    private async Task<ILdapProfile?> LoadUserProfileAsync(
        UserIdentity userIdentity, 
        List<LdapAttributeName> attributes, 
        RadiusPipelineContext context)
    {
        var domainInfo = context.ForestMetadata?.DetermineForestDomain(userIdentity);
        
        if (domainInfo is not null)
        {
            return await LoadProfileFromSpecificDomainAsync(userIdentity, attributes, context, domainInfo);
        }

        return TryGetUserProfile(userIdentity, attributes, context);
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