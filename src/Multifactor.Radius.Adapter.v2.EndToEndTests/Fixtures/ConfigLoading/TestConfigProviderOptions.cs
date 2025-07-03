namespace Multifactor.Radius.Adapter.v2.EndToEndTests.Fixtures.ConfigLoading;

internal class TestConfigProviderOptions
{
    public string? RootConfigFilePath { get; set; }
    public string? ClientConfigsFolderPath { get; set; }
    public string[] ClientConfigFilePaths { get; set; } = [];
}
