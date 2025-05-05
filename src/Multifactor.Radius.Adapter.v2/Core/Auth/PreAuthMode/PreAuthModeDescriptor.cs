namespace Multifactor.Radius.Adapter.v2.Core.Auth.PreAuthMode;

public class PreAuthModeDescriptor
{
    public PreAuthMode Mode { get; }
    public PreAuthModeSettings Settings { get; }

    public static PreAuthModeDescriptor Default => new(PreAuthMode.None, PreAuthModeSettings.Default);

    private PreAuthModeDescriptor(PreAuthMode mode, PreAuthModeSettings settings)
    {
        Mode = mode;
        Settings = settings;
    }

    public static PreAuthModeDescriptor Create(string value, PreAuthModeSettings settings)
    {
        if (settings is null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return new PreAuthModeDescriptor(PreAuthMode.None, settings);
        }

        var mode = GetMode(value);
        return new PreAuthModeDescriptor(mode, settings);
    }

    private static PreAuthMode GetMode(string value)
    {
        Enum.TryParse<PreAuthMode>(value, true, out var parsed);
        return parsed;
    }

    public override string ToString() => Mode.ToString();

    public static string DisplayAvailableModes() => string.Join(", ", Enum.GetNames(typeof(PreAuthMode)));
}
