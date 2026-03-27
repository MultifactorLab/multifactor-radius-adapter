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
using Multifactor.Radius.Adapter.v2.Application.Features.LoadLdapForest.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.LoadProfile.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.LoadProfile.Ports;
using Multifactor.Radius.Adapter.v2.Application.SharedPorts;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps;

internal sealed class ProfileLoadingStep : IRadiusPipelineStep
{
    private readonly IProfileSearch _profileSearch;
    private readonly ILoadLdapSchema _loadLdapSchema;
    private readonly ILogger<ProfileLoadingStep> _logger;
    
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
        ILogger<ProfileLoadingStep> logger, 
        ILoadLdapSchema loadLdapSchema)
    {
        _profileSearch = profileSearch ?? throw new ArgumentNullException(nameof(profileSearch));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loadLdapSchema = loadLdapSchema ?? throw new ArgumentNullException(nameof(loadLdapSchema));
    }

    public async Task ExecuteAsync(RadiusPipelineContext context)
    {
        _logger.LogDebug("'{Name}' started", nameof(ProfileLoadingStep));

        if (ShouldSkipStep(context))
            return;

        ValidateContext(context);

        var userIdentity = new UserIdentity(context.RequestPacket.UserName);
        var attributes = GetAttributes(context).ToArray();
        
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
        var clientAddress = GetClientAddress(context);
        _logger.LogWarning(
            "No user name provided in packet '{PacketId}' from '{Client}'", 
            context.RequestPacket.Identifier, 
            clientAddress);
        throw new InvalidOperationException("Username is required");
    }

    private async Task<ILdapProfile?> LoadUserProfileAsync(
        UserIdentity userIdentity, 
        LdapAttributeName[] attributes, 
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
        LdapAttributeName[] attributes,
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

    private LdapConnectionString BuildConnectionStringForDomain(string baseConnectionString, DistinguishedName domain)
    {
        var preConnectionString = new LdapConnectionString(baseConnectionString, true);
        var fqdn = DnToFqdn(domain);
        return CopySchemaAndPort(preConnectionString, fqdn);
    }

    private FindUserDto CreateFindUserRequest(
        string connectionString,
        UserIdentity userIdentity,
        DistinguishedName searchBase,
        ILdapSchema schema,
        LdapAttributeName[] attributes,
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
            AttributeNames = attributes
        };
    }

    private ILdapProfile? TryGetUserProfile(
        UserIdentity userIdentity,
        LdapAttributeName[] attributes,
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

    private static string GetClientAddress(RadiusPipelineContext context)
    {
        return context.RequestPacket.ProxyEndpoint?.Address.ToString() 
               ?? context.RequestPacket.RemoteEndpoint.Address.ToString();
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
        if (metadata is null)
        {
            _logger.LogDebug("No forest metadata available, using schema naming context as fallback");
            return null;
        }

        if (userIdentity.Format == UserIdentityFormat.UserPrincipalName)
        {
            return FindDomainByUpnSuffix(metadata, userIdentity.GetUpnSuffix());
        }

        return userIdentity.Format switch
        {
            UserIdentityFormat.NetBiosName => FindDomainByNetBios(metadata, userIdentity.Identity),
            UserIdentityFormat.SamAccountName => GetRootDomain(metadata),
            _ => null
        };
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
        if (metadata.NetBiosNames.TryGetValue(netBiosName, out var domain))
        {
            _logger.LogDebug("Found domain '{Domain}' for NetBIOS name '{NetBios}'", 
                domain.DnsName, netBiosName);
            return new DistinguishedName(domain.DistinguishedName);
        }

        return null;
    }

    private DistinguishedName? GetRootDomain(IForestMetadata metadata)
    {
        var rootDomain = metadata.Domains.Values
            .FirstOrDefault(d => d.DnsName == metadata.RootDomain);

        if (rootDomain is not null)
        {
            _logger.LogDebug("Using root domain '{Domain}' for SAM account name", rootDomain.DnsName);
            return new DistinguishedName(rootDomain.DistinguishedName);
        }

        return null;
    }

    private static IList<LdapAttributeName> GetAttributes(RadiusPipelineContext context)
    {
        var attributes = new List<LdapAttributeName>(DefaultAttributes);

        AddIfNotEmpty(attributes, context.LdapConfiguration!.IdentityAttribute);
        
        AddReplyAttributes(attributes, context.ClientConfiguration.ReplyAttributes);
        AddPhoneAttributes(attributes, context.LdapConfiguration.PhoneAttributes);

        return attributes;
    }

    private static void AddIfNotEmpty(ICollection<LdapAttributeName> attributes, string? attributeName)
    {
        if (!string.IsNullOrWhiteSpace(attributeName))
        {
            attributes.Add(new LdapAttributeName(attributeName));
        }
    }

    private static void AddReplyAttributes(
        ICollection<LdapAttributeName> attributes, 
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

    private static void AddPhoneAttributes(
        ICollection<LdapAttributeName> attributes, 
        IEnumerable<string> phoneAttributes)
    {
        foreach (var attr in phoneAttributes.Select(x => new LdapAttributeName(x)))
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