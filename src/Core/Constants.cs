namespace MultiFactor.Radius.Adapter.Core
{
    public static class Constants
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
        }
    }
}
