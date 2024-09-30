using System;
using System.Reflection;

namespace MultiFactor.Radius.Adapter.Infrastructure.Configuration.RootLevel;

internal sealed class RootConfigurationFile
{
    private static readonly Lazy<string> _path = new(() =>
    {
        var asm = Assembly.GetAssembly(typeof(RootConfigurationFile));
        if (asm is null)
        {
            throw new Exception("Unable to get assembly to read build file path");
        }
        
        return $"{asm.Location}.config";
    });
    
    public static string ConfigName => System.IO.Path.GetFileNameWithoutExtension(_path.Value);

    public static string Path => _path.Value;
}