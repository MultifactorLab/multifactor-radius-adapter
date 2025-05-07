using System.ComponentModel;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Sections;

public class AppSettingsSection
{
    [Description("multifactor-api-url")]
    public string? MultifactorApiUrl { get; init; }

    [Description("multifactor-api-proxy")]
    public string? MultifactorApiProxy { get; init; }

    [Description("multifactor-api-timeout")]
    public string? MultifactorApiTimeout { get; init; }

    [Description("multifactor-nas-identifier")]
    public string? MultifactorNasIdentifier { get; init; }

    [Description("multifactor-shared-secret")]
    public string? MultifactorSharedSecret { get; init; }

    [Description("sign-up-groups")]
    public string? SignUpGroups { get; init; }

    [Description("bypass-second-factor-when-api-unreachable")]
    public bool BypassSecondFactorWhenApiUnreachable { get; init; } = true;

    [Description("first-factor-authentication-source")]
    public string? FirstFactorAuthenticationSource { get; init; }
    
    [Description("adapter-client-endpoint")]
    public string? AdapterClientEndpoint { get; init; }

    [Description("adapter-server-endpoint")]
    public string? AdapterServerEndpoint { get; init; }

    [Description("nps-server-endpoint")]
    public string? NpsServerEndpoint { get; init; }

    [Description("radius-client-ip")]
    public string? RadiusClientIp { get; init; }

    [Description("radius-client-nas-identifier")]
    public string? RadiusClientNasIdentifier { get; init; }

    [Description("radius-shared-secret")]
    public string? RadiusSharedSecret { get; init; }
    
    [Description("privacy-mode")]
    public string? PrivacyMode { get; init; }

    [Description("pre-authentication-method")]
    public string? PreAuthenticationMethod { get; init; }

    [Description("authentication-cache-lifetime")]
    public string? AuthenticationCacheLifetime { get; init; }

    [Description("authentication-cache-minimal-matching")]
    public bool AuthenticationCacheMinimalMatching { get; init; } = false;

    [Description("invalid-credential-delay")]
    public string? InvalidCredentialDelay { get; init; }
    
    [Description("logging-format")]
    public string? LoggingFormat { get; init; }

    [Description("logging-level")]
    public string? LoggingLevel { get; init; }

    [Description("calling-station-id-attribute")]
    public string? CallingStationIdAttribute { get; init; }

    [Description("console-log-output-template")]
    public string? ConsoleLogOutputTemplate { get; init; }

    [Description("file-log-output-template")]
    public string? FileLogOutputTemplate { get; init; }
}
