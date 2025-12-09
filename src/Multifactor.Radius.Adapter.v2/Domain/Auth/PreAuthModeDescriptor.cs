namespace Multifactor.Radius.Adapter.v2.Domain.Auth;

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

    public static PreAuthModeDescriptor Create(string? value, PreAuthModeSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (string.IsNullOrWhiteSpace(value))
            return new PreAuthModeDescriptor(PreAuthMode.None, settings);

        var mode = Enum.Parse<PreAuthMode>(value, true);
        return new PreAuthModeDescriptor(mode, settings);
    }

    public override string ToString() => Mode.ToString();

    public static string DisplayAvailableModes() => 
        string.Join(", ", Enum.GetNames(typeof(PreAuthMode)));
}