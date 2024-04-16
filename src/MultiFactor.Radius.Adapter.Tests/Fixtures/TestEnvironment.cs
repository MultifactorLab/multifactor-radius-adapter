namespace MultiFactor.Radius.Adapter.Tests.Fixtures;

internal enum TestAssetLocation
{
    RootDirectory,
    ClientsDirectory
}

internal static class TestEnvironment
{
    private static readonly string _appFolder = $"{Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory)}{Path.DirectorySeparatorChar}";
    private static readonly string _assetsFolder = $"{_appFolder}{Path.DirectorySeparatorChar}Assets";

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
