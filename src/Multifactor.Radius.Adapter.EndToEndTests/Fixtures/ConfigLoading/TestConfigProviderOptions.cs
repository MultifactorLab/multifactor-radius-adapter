namespace Multifactor.Radius.Adapter.EndToEndTests.Fixtures.ConfigLoading;

internal class TestConfigProviderOptions
{
    public string? RootConfigFilePath { get; set; }
    public string? ClientConfigsFolderPath { get; set; }
    public string[] ClientConfigFilePaths { get; set; } = [];
    public string? EnvironmentVariablePrefix { get; set; }
}
