using System;

namespace MultiFactor.Radius.Adapter.Configuration.Features.AuthenticatedClientCacheFeature;

public class AuthenticatedClientCacheConfig
{
    public TimeSpan Lifetime { get; }
    public bool MinimalMatching { get; }
    public bool Enabled => Lifetime != TimeSpan.Zero;

    public AuthenticatedClientCacheConfig(TimeSpan lifetime, bool minimalMatching)
    {
        Lifetime = lifetime;
        MinimalMatching = minimalMatching;
    }

    public static AuthenticatedClientCacheConfig Create(string value, bool minimalMatching = false)
    {
        if (string.IsNullOrWhiteSpace(value)) return new AuthenticatedClientCacheConfig(TimeSpan.Zero, minimalMatching);
        return new AuthenticatedClientCacheConfig(TimeSpan.ParseExact(value, @"hh\:mm\:ss", null, System.Globalization.TimeSpanStyles.None), minimalMatching);
    }
}
