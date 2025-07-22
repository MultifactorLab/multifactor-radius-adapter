using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiFactor.Radius.Adapter.Infrastructure.Configuration.Features.AuthenticatedClientCacheFeature;

public record AuthenticatedClientCacheConfig
{
    public TimeSpan Lifetime { get; }
    public bool MinimalMatching { get; }
    public bool Enabled => Lifetime != TimeSpan.Zero;
    public IReadOnlyCollection<string> AuthenticationCacheGroups { get; }

    public static AuthenticatedClientCacheConfig Default => new(TimeSpan.Zero, false);

    public AuthenticatedClientCacheConfig(TimeSpan lifetime, bool minimalMatching, IReadOnlyCollection<string> authenticationCacheGroups = null)
    {
        Lifetime = lifetime;
        MinimalMatching = minimalMatching;
        AuthenticationCacheGroups = authenticationCacheGroups?.Select(x => x.ToLower()).ToArray() ?? [];
    }

    public static AuthenticatedClientCacheConfig Create(string value, bool minimalMatching, string authenticationCacheGroups = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Default;
        }

        var groups = authenticationCacheGroups
            ?.Split([';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.ToLower())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
        
        return new AuthenticatedClientCacheConfig(
            TimeSpan.ParseExact(value, @"hh\:mm\:ss", null, System.Globalization.TimeSpanStyles.None),
            minimalMatching,
            groups);
    }
}