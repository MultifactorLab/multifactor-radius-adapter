//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.XmlAppConfiguration;

/// <summary>
/// Describes a Radius Adapter configuration file.
/// </summary>
public sealed class RadiusConfigurationFile : RadiusConfigurationSource
{
    /// <summary>
    /// Configuration file path.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Configuration file name without extension.
    /// </summary>
    public override string Name { get; }

    /// <summary>
    /// Configuration file name with extension.
    /// </summary>
    public string FileName { get; }

    public RadiusConfigurationFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException($"'{nameof(path)}' cannot be null or whitespace.", nameof(path));
        }

        if (path.IndexOfAny(System.IO.Path.GetInvalidPathChars()) != -1)
        {
            throw new ArgumentException("Invalid configuration path", nameof(path));
        }

        var name = System.IO.Path.GetFileName(path);
        if (!name.EndsWith(".config"))
        {
            throw new ArgumentException("Invalid configuration path", nameof(path));
        }

        Path = path;
        Name = System.IO.Path.GetFileNameWithoutExtension(path);
        FileName = System.IO.Path.GetFileName(path);
    }

    public static implicit operator string(RadiusConfigurationFile path)
    {
        return path?.Path ?? throw new InvalidCastException("Unable to cast NULL ConfigPath to STRING");
    }

    public static implicit operator RadiusConfigurationFile(string path)
    {
        if (path == null)
        {
            throw new InvalidCastException("Unable cast NULL to ConfigPath");
        }

        try
        {
            return new RadiusConfigurationFile(path);
        }
        catch (Exception ex)
        {
            throw new InvalidCastException("Invalid configuration path", ex);
        }
    }

    public override string ToString() => FileName;
}
