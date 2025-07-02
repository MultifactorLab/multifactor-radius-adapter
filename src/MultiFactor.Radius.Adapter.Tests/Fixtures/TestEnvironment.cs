namespace MultiFactor.Radius.Adapter.Tests.Fixtures;

internal enum TestAssetLocation
{
    RootDirectory,
    ClientsDirectory,
    E2EBaseConfigs,
    E2ESensitiveData
}

internal static class TestEnvironment
{
    private static readonly string _appFolder = $"{Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory)}{Path.DirectorySeparatorChar}";
    private static readonly string _assetsFolder = $"{_appFolder}Assets";

    public static string GetAssetPath(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return _assetsFolder;
        return $"{_assetsFolder}{Path.DirectorySeparatorChar}{fileName}";
    }

    public static string GetAssetPath(TestAssetLocation location)
    {
        return location switch
        {
            TestAssetLocation.ClientsDirectory => $"{_assetsFolder}{Path.DirectorySeparatorChar}clients",
            TestAssetLocation.E2EBaseConfigs => $"{_assetsFolder}{Path.DirectorySeparatorChar}E2E{Path.DirectorySeparatorChar}BaseConfigs",
            TestAssetLocation.E2ESensitiveData => $"{_assetsFolder}{Path.DirectorySeparatorChar}E2E{Path.DirectorySeparatorChar}SensitiveData",
            _ => _assetsFolder,
        };
    }

    public static string GetAssetPath(TestAssetLocation location, string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return GetAssetPath(location);
        var s = $"{GetAssetPath(location)}{Path.DirectorySeparatorChar}{Path.Combine(fileName.Split('/', '\\'))}";
        return s;
    }
}
