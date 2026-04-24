using System.ComponentModel;
using System.Xml.Serialization;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Models;

internal sealed class AdapterConfiguration
{
    public string? FileName { get; set; }
    public AppSettingsSection AppSettings { get; init; } = new();

    public List<LdapServerSection> LdapServers { get; init; } = new();

    public RadiusReplySection RadiusReply { get; init; } = new();
}

internal sealed class AppSettingsSection
{
    [Description("multifactor-api-url")]
    public string MultifactorApiUrl { get; set; }
    [Description("multifactor-api-proxy")]
    public string MultifactorApiProxy { get; set; }
    [Description("multifactor-api-timeout")]
    public string MultifactorApiTimeout { get; set; }
    [Description("adapter-server-endpoint")]
    public string AdapterServerEndpoint { get; set; }
    [Description("logging-level")]
    public string LoggingLevel { get; set; }
    [Description("logging-format")]
    public string LoggingFormat { get; set; }
    [Description("syslog-use-tls")]
    public bool SyslogUseTls { get; set; }
    [Description("syslog-server")]
    public string SyslogServer { get; set; }
    [Description("syslog-format")]
    public string SyslogFormat { get; set; }
    [Description("syslog-facility")]
    public string SyslogFacility { get; set; }
    [Description("syslog-app-name")]
    public string SyslogAppName { get; set; }
    [Description("syslog-framer")]
    public string SyslogFramer { get; set; }
    [Description("syslog-output-template")]
    public string? SyslogOutputTemplate { get; set; }
    
    [Description("console-log-output-template")]
    public string? ConsoleLogOutputTemplate { get; set; }
    [Description("file-log-output-template")]
    public string? FileLogOutputTemplate { get; set; }
    [Description("log-file-max-size-bytes")]
    public int? LogFileMaxSizeBytes { get; set; }
    [Description("multifactor-nas-identifier")]
    public string MultifactorNasIdentifier { get; set; }
    [Description("multifactor-shared-secret")]
    public string MultifactorSharedSecret { get; set; }
    [Description("sign-up-groups")]
    public string SignUpGroups { get; set; }
    [Description("bypass-second-factor-when-api-unreachable")]
    public bool? BypassSecondFactorWhenApiUnreachable { get; set; }
    [Description("first-factor-authentication-source")]
    public string FirstFactorAuthenticationSource { get; set; }
    [Description("adapter-client-endpoint")]
    public string AdapterClientEndpoint { get; set; }
    [Description("radius-client-ip")]
    public string RadiusClientIp { get; set; }
    [Description("radius-client-nas-ip")]
    public string RadiusClientNasIp { get; set; }
    [Description("radius-client-nas-identifier")]
    public string RadiusClientNasIdentifier { get; set; }
    [Description("radius-shared-secret")]
    public string RadiusSharedSecret { get; set; }
    [Description("nps-server-endpoint")]
    public string NpsServerEndpoint { get; set; }
    [Description("nps-server-timeout")]
    public string NpsServerTimeout { get; set; }
    [Description("privacy-mode")]
    public string PrivacyMode { get; set; }
    [Description("pre-authentication-method")]
    public string PreAuthenticationMethod { get; set; }
    [Description("authentication-cache-lifetime")]
    public string AuthenticationCacheLifetime { get; set; }
    [Description("invalid-credential-delay")]
    public string InvalidCredentialDelay { get; set; }
    [Description("calling-station-id-attribute")]
    public string CallingStationIdAttribute { get; set; }
    [Description("ip-from-udp")]
    public bool? IpFromUdp { get; set; }
    
    [Description("ip-white-list")]
    public string IpWhiteList { get; set; }
    [Description("access-challenge-password")]
    public bool? AccessChallengePassword { get; set; }
}

internal sealed class LdapServerSection
{
    [Description("connection-string")]
    public required string ConnectionString { get; set; }
    [Description("username")]
    public required string Username { get; set; }
    [Description("password")]
    public required string Password { get; set; }
    [Description("bind-timeout-in-seconds")]
    public int? BindTimeoutSeconds{ get; set; }
    [Description("access-groups")]
    public string AccessGroups { get; set; }
    [Description("second-fa-groups")]
    public string SecondFaGroups { get; set; }
    [Description("second-fa-bypass-groups")]
    public string SecondFaBypassGroups { get; set; }
    [Description("load-nested-groups")]
    public bool? LoadNestedGroups { get; set; }
    [Description("nested-groups-base-dn")]
    public string NestedGroupsBaseDn { get; set; }
    [Description("authentication-cache-groups")]
    public string AuthenticationCacheGroups { get; set; }
    [Description("phone-attributes")]
    public string PhoneAttributes { get; set; }
    [Description("identity-attribute")]
    public string IdentityAttribute { get; set; }
    [Description("requires-upn")]
    public bool RequiresUpn { get; set; }
    [Description("enable-trusted-domains")]
    public bool EnableTrustedDomains { get; set; }
    [Description("enable-alternative-suffixes")]
    public bool EnableAlternativeSuffixes { get; set; }
    [Description("included-domains")]
    public string? IncludedDomains { get; set; }
    [Description("excluded-domains")]
    public string? ExcludedDomains { get; set; }
    [Description("included-suffixes")]
    public string? IncludedSuffixes { get; set; }
    [Description("excluded-suffixes")]
    public string? ExcludedSuffixes { get; set; }
    [Description("bypass-second-factor-when-api-unreachable-groups")]
    public string? BypassSecondFactorWhenApiUnreachableGroups { get; set; }
}

internal sealed class RadiusReplySection
{
    [XmlArray("Attributes")]
    [XmlArrayItem("add")]
    public List<RadiusAttributeItem> Attributes { get; set; }
}

internal sealed class RadiusAttributeItem
{
    [Description("name")]
    public string Name { get; set; }
    
    [Description("from")]
    public string From { get; set; }
    
    [Description("value")]
    public string Value { get; set; }
    
    [Description("when")]
    public string When { get; set; }
    
    [Description("sufficient")]
    public string Sufficient { get; set; }
}