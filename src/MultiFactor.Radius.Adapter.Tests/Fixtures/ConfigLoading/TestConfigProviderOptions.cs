namespace MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;

internal class TestConfigProviderOptions
{
    public string? RootConfigFilePath { get; set; }
    public string? ClientConfigsFolderPath { get; set; }
    public string[] ClientConfigFilePaths { get; set; } = Array.Empty<string>();
    public string EnvironmentVariablePrefix { get; set; }
}
