using System.Collections;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Reader;

public static class EnvironmentReader
{
    public static IReadOnlyDictionary<string, string> ReadEnvironments(string? prefix = null)
    {
        return Environment.GetEnvironmentVariables()
            .Cast<DictionaryEntry>()
            .Where(x => x.Key.ToString().StartsWith(prefix))
            .ToDictionary(x => x.Key.ToString(), x => x.Value.ToString());
    }
}