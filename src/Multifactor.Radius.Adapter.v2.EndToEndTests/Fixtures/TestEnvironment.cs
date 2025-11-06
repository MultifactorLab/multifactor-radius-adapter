namespace Multifactor.Radius.Adapter.v2.EndToEndTests.Fixtures;

internal enum TestAssetLocation
{
    RootDirectory,
    ClientsDirectory,
    E2EBaseConfigs,
    E2ESensitiveData
}

internal static class TestEnvironment
{
    private static readonly string AppFolder = $"{Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory)}{Path.DirectorySeparatorChar}";
    private static readonly string AssetsFolder = $"{AppFolder}Assets";

    public static string GetAssetPath(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return AssetsFolder;
        return $"{AssetsFolder}{Path.DirectorySeparatorChar}{fileName}";
    }

    public static string GetAssetPath(TestAssetLocation location)
    {
        return location switch
        {
            TestAssetLocation.ClientsDirectory => $"{AssetsFolder}{Path.DirectorySeparatorChar}clients",
            TestAssetLocation.E2EBaseConfigs => $"{AssetsFolder}{Path.DirectorySeparatorChar}BaseConfigs",
            TestAssetLocation.E2ESensitiveData => $"{AssetsFolder}{Path.DirectorySeparatorChar}SensitiveData",
            _ => AssetsFolder,
        };
    }

    public static string GetAssetPath(TestAssetLocation location, string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return GetAssetPath(location);
        var s = $"{GetAssetPath(location)}{Path.DirectorySeparatorChar}{Path.Combine(fileName.Split('/', '\\'))}";
        return s;
    }
}
