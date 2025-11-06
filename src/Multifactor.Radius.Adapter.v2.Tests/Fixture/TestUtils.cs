using System.Reflection;
using Multifactor.Radius.Adapter.v2.Core;
using Multifactor.Radius.Adapter.v2.Core.Radius.Attributes;

namespace Multifactor.Radius.Adapter.v2.Tests.Fixture;

internal static class TestUtils
{
    public static IRadiusDictionary GetRadiusDictionary(string? path = null)
    {
        var appVars = new ApplicationVariables()
        {
            AppPath = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory),
            AppVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(),
        };

        var dictionarySourcePath = path ?? $"{Path.DirectorySeparatorChar}Assets{Path.DirectorySeparatorChar}content{Path.DirectorySeparatorChar}radius.dictionary";
        var dictionary = new RadiusDictionary(appVars, dictionarySourcePath);
        dictionary.Read();
        return dictionary;
    }
}