//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using System;
using System.IO;
using System.Reflection;

namespace MultiFactor.Radius.Adapter.Configuration.ConfigurationLoading;

internal static class RootConfigurationFile
{
    private const string _resName = "App.config";
    private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();
    private static readonly Lazy<string> _content = new Lazy<string>(() => ReadResource(_resName, _assembly));

    /// <summary>
    /// Creates a root config file if it does not exist or does nothing.
    /// </summary>
    public static void Touch()
    {
        var asm = Assembly.GetExecutingAssembly();
        var path = $"{asm.Location}.config";
        if (!File.Exists(path))
        {
            File.WriteAllText(path, _content.Value, System.Text.Encoding.UTF8);
        }
    }

    private static string ReadResource(string resName, Assembly asm)
    {
        var type = asm.GetType(typeof(RootType).FullName);
        var path = $"{type.Namespace}.{resName}";

        using var stream = asm.GetManifestResourceStream(path);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}