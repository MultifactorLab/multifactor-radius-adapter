namespace MultiFactor.Radius.Adapter.Core
{
    public static class Literals
    {
        public static class Configuration
        {
            public const string ActiveDirectory2FaBypassGroup = "active-directory-2fa-bypass-group";
            public const string ActiveDirectory2FaGroup = "active-directory-2fa-group";
            public const string ActiveDirectoryDomain = "active-directory-domain";
            public const string ActiveDirectoryGroup = "active-directory-group";
            public const string AdapterClientEndpoint = "adapter-client-endpoint";
            public const string AdapterServerEndpoint = "adapter-server-endpoint";
            public const string AuthenticationCacheLifetime = "authentication-cache-lifetime";
            public const string AuthenticationCacheMinimalMatching = "authentication-cache-minimal-matching";
            public const string BypassSecondFactorWhenApiUnreachable = "bypass-second-factor-when-api-unreachable";
            public const string CallingStationIdAttribute = "calling-station-id-attribute";
            public const string ConsoleLogOutputTemplate = "console-log-output-template";
            public const string FileLogOutputTemplate = "file-log-output-template";
            public const string FirstFactorAuthSource = "first-factor-authentication-source";
            public const string InvalidCredentialDelay = "invalid-credential-delay";
            public const string LdapBindDn = "ldap-bind-dn";
            public const string LoadActiveDirectoryNestedGroups = "load-active-directory-nested-groups";
            public const string LoggingFormat = "logging-format";
            public const string LoggingLevel = "logging-level";
            public const string MultifactorApiProxy = "multifactor-api-proxy";
            public const string MultifactorApiUrl = "multifactor-api-url";
            public const string MultifactorApiTimeout= "multifactor-api-timeout";
            public const string MultifactorNasIdentifier = "multifactor-nas-identifier";
            public const string MultifactorSharedSecret = "multifactor-shared-secret";
            public const string NpsServerEndpoint = "nps-server-endpoint";
            public const string PhoneAttribute = "phone-attribute";
            public const string PrivacyMode = "privacy-mode";
            public const string RadiusClientIp = "radius-client-ip";
            public const string RadiusClientNasIdentifier = "radius-client-nas-identifier";
            public const string RadiusSharedSecret = "radius-shared-secret";
            public const string ServiceAccountPassword = "service-account-password";
            public const string ServiceAccountUser = "service-account-user";
            public const string SignUpGroups = "sign-up-groups";
            public const string UseActiveDirectoryMobileUserPhone = "use-active-directory-mobile-user-phone";
            public const string UseActiveDirectoryUserPhone = "use-active-directory-user-phone";
            public const string UseUpnAsIdentity = "use-upn-as-identity";
        }
    }
}
