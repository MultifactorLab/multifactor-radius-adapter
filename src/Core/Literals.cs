namespace MultiFactor.Radius.Adapter.Core
{
    public static class Literals
    {
        public static class Configuration
        {
            public const string FileLogOutputTemplate = "file-log-output-template";
            public const string ConsoleLogOutputTemplate = "console-log-output-template";

            public static class PciDss
            {
                public const string InvalidCredentialDelay = "invalid-credential-delay";
            }

            public const string AuthenticationCacheLifetime = "authentication-cache-lifetime";
            public const string AuthenticationCacheMinimalMatching = "authentication-cache-minimal-matching";

            public const string CallingStationIdAttribute = "calling-station-id-attribute";
            public const string MultifactorApiProxy = "multifactor-api-proxy";
        }
    }
}
