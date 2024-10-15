//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using System;
using System.ComponentModel;

namespace MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models;

public class AppSettingsSection
{
    [Description("multifactor-api-url")]
    public string MultifactorApiUrl { get; init; }

    [Description("multifactor-api-proxy")]
    public string MultifactorApiProxy { get; init; }

    [Description("multifactor-api-timeout")]
    public string MultifactorApiTimeout { get; init; }

    [Description("multifactor-nas-identifier")]
    public string MultifactorNasIdentifier { get; init; }

    [Description("multifactor-shared-secret")]
    public string MultifactorSharedSecret { get; init; }

    [Description("sign-up-groups")]
    public string SignUpGroups { get; init; }

    [Description("bypass-second-factor-when-api-unreachable")]
    public bool BypassSecondFactorWhenApiUnreachable { get; init; } = true;

    [Description("first-factor-authentication-source")]
    public string FirstFactorAuthenticationSource { get; init; }



    [Description("active-directory-domain")]
    public string ActiveDirectoryDomain { get; init; }

    [Description("active-directory-2fa-bypass-group")]
    public string ActiveDirectory2faBypassGroup { get; init; }

    [Description("active-directory-2fa-group")]
    public string ActiveDirectory2faGroup { get; init; }

    [Description("active-directory-group")]
    public string ActiveDirectoryGroup { get; init; }

    [Description("ldap-bind-dn")]
    public string LdapBindDn { get; init; }

    [Description("load-active-directory-nested-groups")]
    public bool LoadActiveDirectoryNestedGroups { get; init; } = true;

    [Description("nested-groups-base-dn")]
    public string NestedGroupsBaseDn { get; init; }

    [Description("phone-attribute")]
    public string PhoneAttribute { get; init; }

    [Description("use-active-directory-user-phone")]
    public bool UseActiveDirectoryUserPhone { get; init; } = false;

    [Description("use-active-directory-mobile-user-phone")]
    public bool UseActiveDirectoryMobileUserPhone { get; init; } = false;

    [Description("use-upn-as-identity")]
    public string UseUpnAsIdentity { get; init; }

    [Description("use-attribute-as-identity")]
    public string UseAttributeAsIdentity { get; init; }

    [Description("service-account-user")]
    public string ServiceAccountUser { get; init; }

    [Description("service-account-password")]
    public string ServiceAccountPassword { get; init; }



    [Description("adapter-client-endpoint")]
    public string AdapterClientEndpoint { get; init; }

    [Description("adapter-server-endpoint")]
    public string AdapterServerEndpoint { get; init; }

    [Description("nps-server-endpoint")]
    public string NpsServerEndpoint { get; init; }

    [Description("radius-client-ip")]
    public string RadiusClientIp { get; init; }

    [Description("radius-client-nas-identifier")]
    public string RadiusClientNasIdentifier { get; init; }

    [Description("radius-shared-secret")]
    public string RadiusSharedSecret { get; init; }



    [Description("privacy-mode")]
    public string PrivacyMode { get; init; }

    [Description("pre-authentication-method")]
    public string PreAuthenticationMethod { get; init; }

    [Description("authentication-cache-lifetime")]
    public string AuthenticationCacheLifetime { get; init; }

    [Description("authentication-cache-minimal-matching")]
    public bool AuthenticationCacheMinimalMatching { get; init; } = false;

    [Description("invalid-credential-delay")]
    public string InvalidCredentialDelay { get; init; }



    [Description("logging-format")]
    public string LoggingFormat { get; init; }

    [Description("logging-level")]
    public string LoggingLevel { get; init; }

    [Description("calling-station-id-attribute")]
    public string CallingStationIdAttribute { get; init; }

    [Description("console-log-output-template")]
    public string ConsoleLogOutputTemplate { get; init; }

    [Description("file-log-output-template")]
    public string FileLogOutputTemplate { get; init; }
    
    [Description("ldap-bind-timeout")]
    public TimeSpan LdapBindTimeout { get; set; } = new TimeSpan(0, 0, 30);
}
