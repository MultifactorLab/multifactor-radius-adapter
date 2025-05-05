using System.Net;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.Auth.PreAuthMode;
using Multifactor.Radius.Adapter.v2.Core.MultifactorApi;
using Multifactor.Radius.Adapter.v2.Core.MultifactorApi.PrivacyMode;
using Multifactor.Radius.Adapter.v2.Core.RandomWaiterFeature;

namespace Multifactor.Radius.Adapter.v2.Core.Configuration.Client;

public class ClientConfiguration : IClientConfiguration
{
    private List<ILdapServerConfiguration> _ldapServers = new();

    public IReadOnlyList<ILdapServerConfiguration> LdapServers => _ldapServers;

    public ClientConfiguration(string name,
        string rdsSharedSecret,
        AuthenticationSource firstFactorAuthSource,
        string apiKey,
        string apiSecret)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException($"'{nameof(name)}' cannot be null or whitespace.", nameof(name));

        if (string.IsNullOrWhiteSpace(rdsSharedSecret))
            throw new ArgumentException($"'{nameof(rdsSharedSecret)}' cannot be null or whitespace.",
                nameof(rdsSharedSecret));

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException($"'{nameof(apiKey)}' cannot be null or whitespace.", nameof(apiKey));

        if (string.IsNullOrWhiteSpace(apiSecret))
            throw new ArgumentException($"'{nameof(apiSecret)}' cannot be null or whitespace.", nameof(apiSecret));

        BypassSecondFactorWhenApiUnreachable = true; //by default

        Name = name;
        RadiusSharedSecret = rdsSharedSecret;
        FirstFactorAuthenticationSource = firstFactorAuthSource;
        ApiCredential = new ApiCredential(apiKey, apiSecret);
    }

    /// <summary>
    /// Friendly client name
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Shared secret between this service and Radius client
    /// </summary>
    public string RadiusSharedSecret { get; }

    /// <summary>
    /// Where to handle first factor (UserName and Password)
    /// </summary>
    public AuthenticationSource FirstFactorAuthenticationSource { get; }

    public ApiCredential ApiCredential { get; }

    /// <summary>
    /// Bypass second factor when MultiFactor API is unreachable
    /// </summary>
    public bool BypassSecondFactorWhenApiUnreachable { get; private set; }

    public PrivacyModeDescriptor PrivacyMode { get; private set; } = PrivacyModeDescriptor.Default;

    /// <summary>
    /// This service RADIUS UDP Client endpoint
    /// </summary>
    public IPEndPoint ServiceClientEndpoint { get; private set; }

    /// <summary>
    /// Network Policy Service RADIUS UDP Server endpoint
    /// </summary>
    public IPEndPoint NpsServerEndpoint { get; private set; }

    /// <summary>
    /// Groups to assign to the registered user.Specified groups will be assigned to a new user.
    /// Syntax: group names (from your Management Portal) separated by semicolons.
    /// <para>
    /// Example: group1;Group Name Two;
    /// </para>
    /// </summary>
    public string SignUpGroups { get; private set; }

    public AuthenticatedClientCacheConfig AuthenticationCacheLifetime { get; private set; } =
        AuthenticatedClientCacheConfig.Default;

    private readonly Dictionary<string, RadiusReplyAttributeValue[]> _radiusReplyAttributes = new();

    /// <summary>
    /// Custom RADIUS reply attributes
    /// </summary>
    public IReadOnlyDictionary<string, RadiusReplyAttributeValue[]> RadiusReplyAttributes => _radiusReplyAttributes;

    /// <summary>
    /// Username transform rules
    /// </summary>
    public UserNameTransformRules UserNameTransformRules { get; private set; } = new();

    public ClientConfiguration SetUserNameTransformRules(UserNameTransformRules val)
    {
        UserNameTransformRules = val;
        return this;
    }

    public string CallingStationIdVendorAttribute { get; private set; }

    public RandomWaiterConfig InvalidCredentialDelay { get; private set; }
    public PreAuthModeDescriptor PreAuthnMode { get; private set; } = PreAuthModeDescriptor.Default;

    public ClientConfiguration SetBypassSecondFactorWhenApiUnreachable(bool val)
    {
        BypassSecondFactorWhenApiUnreachable = val;
        return this;
    }

    public ClientConfiguration SetPrivacyMode(PrivacyModeDescriptor val)
    {
        PrivacyMode = val;
        return this;
    }

    public ClientConfiguration SetServiceClientEndpoint(IPEndPoint val)
    {
        ServiceClientEndpoint = val;
        return this;
    }

    public ClientConfiguration SetNpsServerEndpoint(IPEndPoint val)
    {
        NpsServerEndpoint = val;
        return this;
    }

    public ClientConfiguration SetSignUpGroups(string val)
    {
        SignUpGroups = val;
        return this;
    }

    public ClientConfiguration SetAuthenticationCacheLifetime(AuthenticatedClientCacheConfig val)
    {
        AuthenticationCacheLifetime = val;
        return this;
    }

    public ClientConfiguration AddRadiusReplyAttribute(string attr, IEnumerable<RadiusReplyAttributeValue> values)
    {
        if (string.IsNullOrWhiteSpace(attr))
        {
            throw new ArgumentException($"'{nameof(attr)}' cannot be null or whitespace.", nameof(attr));
        }

        if (values is null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        _radiusReplyAttributes[attr] = values.ToArray();
        return this;
    }

    public ClientConfiguration SetCallingStationIdVendorAttribute(string val)
    {
        if (string.IsNullOrWhiteSpace(val))
        {
            throw new ArgumentException($"'{nameof(val)}' cannot be null or whitespace.", nameof(val));
        }

        CallingStationIdVendorAttribute = val;
        return this;
    }

    public ClientConfiguration SetInvalidCredentialDelay(RandomWaiterConfig val)
    {
        InvalidCredentialDelay = val ?? throw new ArgumentNullException(nameof(val));
        return this;
    }

    public ClientConfiguration SetPreAuthMode(PreAuthModeDescriptor val)
    {
        PreAuthnMode = val ?? throw new ArgumentNullException(nameof(val));
        return this;
    }

    public ClientConfiguration AddLdapServers(params ILdapServerConfiguration[] ldapServers)
    {
        if (ldapServers?.Length > 0)
            _ldapServers.AddRange(ldapServers);
        else
            throw new ArgumentNullException(nameof(ldapServers));
        return this;
    }
}