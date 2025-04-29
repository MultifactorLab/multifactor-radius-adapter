namespace Multifactor.Radius.Adapter.v2.Core.Auth;

public record AuthenticatedClientCacheConfig
{
    public TimeSpan Lifetime { get; }
    public bool MinimalMatching { get; }
    public bool Enabled => Lifetime != TimeSpan.Zero;

    public static AuthenticatedClientCacheConfig Default => new(TimeSpan.Zero, false);

    public AuthenticatedClientCacheConfig(TimeSpan lifetime, bool minimalMatching)
    {
        Lifetime = lifetime;
        MinimalMatching = minimalMatching;
    }

    public static AuthenticatedClientCacheConfig Create(string value, bool minimalMatching)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Default;
        }

        return new AuthenticatedClientCacheConfig(TimeSpan.ParseExact(value, @"hh\:mm\:ss", null, System.Globalization.TimeSpanStyles.None), minimalMatching);
    }
}
