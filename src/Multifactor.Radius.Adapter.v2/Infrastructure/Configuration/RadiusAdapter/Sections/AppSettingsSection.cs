using System.ComponentModel;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Sections;

public class AppSettingsSection
{
    [Description("multifactor-api-url")]
    public string MultifactorApiUrl { get; init; } = string.Empty;

    [Description("multifactor-api-proxy")]
    public string MultifactorApiProxy { get; init; } = string.Empty;

    [Description("multifactor-api-timeout")]
    public string MultifactorApiTimeout { get; init; } = string.Empty;

    [Description("multifactor-nas-identifier")]
    public string MultifactorNasIdentifier { get; init; } = string.Empty;

    [Description("multifactor-shared-secret")]
    public string MultifactorSharedSecret { get; init; } = string.Empty;

    [Description("sign-up-groups")] 
    public string SignUpGroups { get; init; } = string.Empty;

    [Description("bypass-second-factor-when-api-unreachable")]
    public bool BypassSecondFactorWhenApiUnreachable { get; init; } = true;

    [Description("first-factor-authentication-source")]
    public string FirstFactorAuthenticationSource { get; init; } = string.Empty;

    [Description("adapter-client-endpoint")]
    public string AdapterClientEndpoint { get; init; } = string.Empty;

    [Description("adapter-server-endpoint")]
    public string AdapterServerEndpoint { get; init; } = string.Empty;

    [Description("nps-server-endpoint")]
    public string NpsServerEndpoint { get; init; } = string.Empty;

    [Description("radius-client-ip")]
    public string RadiusClientIp { get; init; } = string.Empty;

    [Description("radius-client-nas-identifier")]
    public string RadiusClientNasIdentifier { get; init; } = string.Empty;

    [Description("radius-shared-secret")]
    public string RadiusSharedSecret { get; init; } = string.Empty;

    [Description("privacy-mode")]
    public string PrivacyMode { get; init; } = string.Empty;

    [Description("pre-authentication-method")]
    public string PreAuthenticationMethod { get; init; } = string.Empty;

    [Description("authentication-cache-lifetime")]
    public string AuthenticationCacheLifetime { get; init; } = string.Empty;

    [Description("authentication-cache-minimal-matching")]
    public bool AuthenticationCacheMinimalMatching { get; init; } = false;

    [Description("invalid-credential-delay")]
    public string InvalidCredentialDelay { get; init; } = string.Empty;

    [Description("logging-format")]
    public string LoggingFormat { get; init; } = string.Empty;

    [Description("logging-level")]
    public string LoggingLevel { get; init; } = string.Empty;

    [Description("calling-station-id-attribute")]
    public string CallingStationIdAttribute { get; init; } = string.Empty;

    [Description("console-log-output-template")]
    public string ConsoleLogOutputTemplate { get; init; } = string.Empty;

    [Description("file-log-output-template")]
    public string FileLogOutputTemplate { get; init; } = string.Empty;
}