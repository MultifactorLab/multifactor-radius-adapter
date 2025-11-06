using System.Reflection;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter;

internal sealed class RadiusAdapterConfigurationFile
{
    private static readonly Lazy<string> _path = new(() =>
    {
        var asm = Assembly.GetAssembly(typeof(RadiusAdapterConfigurationFile));
        if (asm is null)
        {
            throw new Exception("Unable to get assembly to read build file path");
        }
        
        return $"{asm.Location}.config";
    });
    
    public static string ConfigName => System.IO.Path.GetFileNameWithoutExtension(_path.Value);

    public static string Path => _path.Value;
}