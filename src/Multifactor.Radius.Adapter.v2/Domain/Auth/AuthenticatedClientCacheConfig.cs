namespace Multifactor.Radius.Adapter.v2.Domain.Auth;

public record AuthenticatedClientCacheConfig
{
    public TimeSpan Lifetime { get; }
    public bool Enabled => Lifetime > TimeSpan.Zero;

    public static AuthenticatedClientCacheConfig Default => new(TimeSpan.Zero);

    public AuthenticatedClientCacheConfig(TimeSpan lifetime)
    {
        Lifetime = lifetime;
    }

    public static AuthenticatedClientCacheConfig Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Default;

        return new AuthenticatedClientCacheConfig(
            TimeSpan.ParseExact(value, @"hh\:mm\:ss", null, System.Globalization.TimeSpanStyles.None));
    }

    public static AuthenticatedClientCacheConfig FromMinutes(int minutes) => 
        new(TimeSpan.FromMinutes(minutes));
}