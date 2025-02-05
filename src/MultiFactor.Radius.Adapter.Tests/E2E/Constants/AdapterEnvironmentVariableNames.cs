using MultiFactor.Radius.Adapter.Infrastructure.Configuration.ConfigurationLoading;

namespace MultiFactor.Radius.Adapter.Tests.E2E.Constants;

public static class AdapterEnvironmentVariableNames
{
    public const string ActiveDirectoryDomain = "ActiveDirectoryDomain";
    public const string ServiceAccountUser = "ServiceAccountUser";
    public const string ServiceAccountPassword = "ServiceAccountPassword";
    public const string BypassSecondFactorWhenApiUnreachable = "BypassSecondFactorWhenApiUnreachable";
    
    public static string GetEnvironmentVariableName(string targetConfig, string environmentVariableName) => $"{ConfigurationBuilderExtensions.BasePrefix}{targetConfig}_APPSETTINGS:{environmentVariableName}";
}