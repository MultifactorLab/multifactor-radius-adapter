using System;
using System.Reflection;

namespace MultiFactor.Radius.Adapter.Infrastructure.Configuration.RootLevel;

internal sealed class RootConfigurationFile
{
    private static readonly Lazy<string> _path = new(() =>
    {
        var asm = Assembly.GetAssembly(typeof(RootConfigurationFile));
        var path = $"{asm.Location}.config";
        return path;
    });
    
    public static string ConfigName => System.IO.Path.GetFileNameWithoutExtension(_path.Value);

    public static string Path => _path.Value;
}