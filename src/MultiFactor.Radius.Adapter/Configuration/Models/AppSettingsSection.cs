//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Configuration.Features.PreAuthModeFeature;
namespace MultiFactor.Radius.Adapter.Configuration.Models;

public class AppSettingsSection
{
    public string MultifactorApiUrl { get; init; }
    public string MultifactorApiProxy { get; init; }
    public string MultifactorApiTimeout { get; init; }
    public string MultifactorNasIdentifier { get; init; }
    public string MultifactorSharedSecret { get; init; }
    public string SignUpGroups { get; init; }
    public bool BypassSecondFactorWhenApiUnreachable { get; init; } = true;


    public AuthenticationSource FirstFactorAuthenticationSource { get; init; }


    public string ActiveDirectoryDomain { get; init; }
    public string ActiveDirectory2faBypassGroup { get; init; }
    public string ActiveDirectory2faGroup { get; init; }
    public string ActiveDirectoryGroup { get; init; }
    public string LdapBindDn { get; init; }
    public bool LoadActiveDirectoryNestedGroups { get; init; } = false;
    public bool UseActiveDirectoryMobileUserPhone { get; init; } = false;
    public bool UseActiveDirectoryUserPhone { get; init; } = false;
    public bool UseUpnAsIdentity { get; init; } = false;
    public string UseAttributeAsIdentity { get; init; }
    public string PhoneAttribute { get; init; }
    public string ServiceAccountPassword { get; init; }
    public string ServiceAccountUser { get; init; }


    public string AdapterClientEndpoint { get; init; }
    public string AdapterServerEndpoint { get; init; }
    public string NpsServerEndpoint { get; init; }
    public string RadiusClientIp { get; init; }
    public string RadiusClientNasIdentifier { get; init; }
    public string RadiusSharedSecret { get; init; }


    public string PrivacyMode { get; init; }
    public PreAuthMode PreAuthenticationMethod { get; init; }
    public string AuthenticationCacheLifetime { get; init; }
    public bool AuthenticationCacheMinimalMatching { get; init; } = false;
    public string InvalidCredentialDelay { get; init; }


    public string LoggingFormat { get; init; }
    public string LoggingLevel { get; init; }
    public string CallingStationIdAttribute { get; init; }
    public string ConsoleLogOutputTemplate { get; init; }
    public string FileLogOutputTemplate { get; init; }
}
