namespace Multifactor.Radius.Adapter.v2.Tests.Fixture;

public static class ConfigUtils
{
    internal static Dictionary<string, string> GetConfigSensitiveData(string fileName, string separator = ":")
    {
        var sensitiveDataPath = TestEnvironment.GetAssetPath(TestAssetLocation.SensitiveData, fileName);

        var lines = File.ReadLines(sensitiveDataPath);
        var sensitiveData = new Dictionary<string, string>();

        foreach (var line in lines)
        {
            var parts = line.Split(separator, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
                throw new ArgumentException($"Invalid sensitive data line: {line}");
            sensitiveData.Add(parts[0], parts[1]);
        }

        return sensitiveData;
    }
}