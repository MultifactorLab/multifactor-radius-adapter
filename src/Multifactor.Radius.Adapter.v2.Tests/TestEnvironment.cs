namespace Multifactor.Radius.Adapter.v2.Tests;

internal enum TestAssetLocation
{
    RootDirectory,
    ClientsDirectory,
    SensitiveData
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
            TestAssetLocation.SensitiveData => $"{_assetsFolder}{Path.DirectorySeparatorChar}SensitiveData",
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
