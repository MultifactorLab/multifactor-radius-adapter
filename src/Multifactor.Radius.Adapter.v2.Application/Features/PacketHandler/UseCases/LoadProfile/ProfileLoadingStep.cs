using System.DirectoryServices.Protocols;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Attributes;
using Multifactor.Core.Ldap.Name;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
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
    private readonly ILoadLdapSchema _loadLdapSchema;
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
        ILoadLdapSchema loadLdapSchema,
        ILogger<ProfileLoadingStep> logger)
    {
        _profileSearch = profileSearch;
        _loadLdapSchema = loadLdapSchema;
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
        ArgumentNullException.ThrowIfNull(context.LdapSchema);

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
        var searchBase = DetermineForestDomain(context.ForestMetadata, userIdentity);
        
        if (searchBase is not null)
        {
            return await LoadProfileFromSpecificDomainAsync(userIdentity, attributes, context, searchBase);
        }

        return TryGetUserProfile(userIdentity, attributes, context);
    }

    private Task<ILdapProfile?> LoadProfileFromSpecificDomainAsync(
        UserIdentity userIdentity,
        List<LdapAttributeName> attributes,
        RadiusPipelineContext context,
        DistinguishedName searchBase)
    {
        var connectionString = BuildConnectionStringForDomain(context.LdapConfiguration!.ConnectionString, searchBase);
        
        var schema = LoadSchemaForDomainAsync(connectionString, context);
        if (schema is null)
        {
            _logger.LogWarning("Failed to load schema for domain '{Domain}'", searchBase.StringRepresentation);
            return Task.FromResult<ILdapProfile?>(null);
        }

        var request = CreateFindUserRequest(
            connectionString.ToString(),
            userIdentity,
            searchBase,
            schema,
            attributes,
            context.LdapConfiguration);

        var profile = _profileSearch.Execute(request);
        
        if (profile is not null)
        {
            _logger.LogDebug("Found profile in specific domain '{Domain}'", searchBase.StringRepresentation);
        }

        return Task.FromResult(profile);
    }

    private ILdapSchema? LoadSchemaForDomainAsync(
        LdapConnectionString connectionString,
        RadiusPipelineContext context)
    {
        var dto = new LoadLdapSchemaDto(connectionString.ToString(),
            ConvertToUpn(context.RequestPacket.UserName),
            context.LdapConfiguration!.Password,
            context.LdapConfiguration.BindTimeoutSeconds,
            AuthType.Negotiate);

        return _loadLdapSchema.Execute(dto);
    }

    private static LdapConnectionString BuildConnectionStringForDomain(string baseConnectionString, DistinguishedName domain)
    {
        var preConnectionString = new LdapConnectionString(baseConnectionString, true);
        var fqdn = DnToFqdn(domain);
        return CopySchemaAndPort(preConnectionString, fqdn);
    }

    private static FindUserDto CreateFindUserRequest(
        string connectionString,
        UserIdentity userIdentity,
        DistinguishedName searchBase,
        ILdapSchema schema,
        List<LdapAttributeName> attributes,
        ILdapServerConfiguration config)
    {
        return new FindUserDto
        {
            ConnectionString = connectionString,
            UserName = config.Username,
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
            userIdentity,
            context.LdapSchema!.NamingContext,
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
        return profile.Dn?.StringRepresentation 
               ?? context.LdapSchema?.NamingContext?.StringRepresentation 
               ?? "unknown";
    }

    private static string GetSearchBaseInfo(RadiusPipelineContext context)
    {
        return context.ForestMetadata?.RootDomain 
               ?? context.LdapSchema?.NamingContext?.StringRepresentation 
               ?? "unknown";
    }

    private static string ConvertToUpn(string username)
    {
        if (string.IsNullOrEmpty(username))
            throw new ArgumentNullException(nameof(username));

        // Если уже UPN
        if (username.Contains('@') && Regex.IsMatch(username, @"^[^@]+@[^@]+\.[^@]+$"))
            return username;

        // Если DN
        if (username.Contains('=') && (username.Contains("CN=") || username.Contains("DC=")))
        {
            var domain = ExtractDomainFromDn(username);
            var cn = ExtractCn(username);
            return string.IsNullOrEmpty(domain) || string.IsNullOrEmpty(cn) 
                ? username 
                : $"{cn}@{domain}";
        }

        return username;
    }

    private static string ExtractDomainFromDn(string distinguishedName)
    {
        return string.Join(".", 
            distinguishedName.Split(',')
                .Select(p => p.Trim())
                .Where(p => p.StartsWith("DC=", StringComparison.OrdinalIgnoreCase))
                .Select(p => p[3..]));
    }

    private static string ExtractCn(string distinguishedName)
    {
        return distinguishedName.Split(',')
            .Select(p => p.Trim())
            .FirstOrDefault(p => p.StartsWith("CN=", StringComparison.OrdinalIgnoreCase))
            ?[3..] ?? string.Empty;
    }

    private DistinguishedName? DetermineForestDomain(IForestMetadata? metadata, UserIdentity userIdentity)
    {
        if (metadata is not null)
            return userIdentity.Format switch
            {
                UserIdentityFormat.UserPrincipalName => FindDomainByUpnSuffix(metadata, userIdentity.GetUpnSuffix()),
                UserIdentityFormat.NetBiosName => FindDomainByNetBios(metadata, userIdentity.Identity),
                UserIdentityFormat.SamAccountName => GetRootDomain(metadata),
                _ => null
            };
        _logger.LogDebug("No forest metadata available, using schema naming context as fallback");
        return null;
    }

    private DistinguishedName? FindDomainByUpnSuffix(IForestMetadata metadata, string suffix)
    {
        if (metadata.UpnSuffixes.TryGetValue(suffix, out var domain))
        {
            _logger.LogDebug("Found domain '{Domain}' for UPN suffix '{Suffix}'", domain.DnsName, suffix);
            return new DistinguishedName(domain.DistinguishedName);
        }

        // Partial match
        foreach (var kv in metadata.UpnSuffixes)
        {
            if (!suffix.EndsWith(kv.Key)) continue;
            
            _logger.LogDebug("Found partial match: domain '{Domain}' for suffix '{Suffix}'", 
                kv.Value.DnsName, suffix);
            return new DistinguishedName(kv.Value.DistinguishedName);
        }

        return null;
    }

    private DistinguishedName? FindDomainByNetBios(IForestMetadata metadata, string netBiosName)
    {
        if (!metadata.NetBiosNames.TryGetValue(netBiosName, out var domain)) return null;
        _logger.LogDebug("Found domain '{Domain}' for NetBIOS name '{NetBios}'", 
            domain.DnsName, netBiosName);
        return new DistinguishedName(domain.DistinguishedName);
    }

    private DistinguishedName? GetRootDomain(IForestMetadata metadata)
    {
        var rootDomain = metadata.Domains.Values
            .FirstOrDefault(d => d.DnsName == metadata.RootDomain);

        if (rootDomain is null) return null;
        _logger.LogDebug("Using root domain '{Domain}' for SAM account name", rootDomain.DnsName);
        return new DistinguishedName(rootDomain.DistinguishedName);
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

    private static LdapConnectionString CopySchemaAndPort(LdapConnectionString source, string newHost)
    {
        return new LdapConnectionString($"{source.Scheme}://{newHost}:{source.Port}", true);
    }

    private static string DnToFqdn(DistinguishedName name)
    {
        return string.Join(".", name.Components.Reverse().Select(x => x.Value));
    }
}