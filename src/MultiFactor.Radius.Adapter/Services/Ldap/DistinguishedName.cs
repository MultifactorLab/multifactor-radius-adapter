using System;

namespace MultiFactor.Radius.Adapter.Services.Ldap;

public record DistinguishedName
{
    public string Value { get; }
    public string Name { get; }

    private DistinguishedName(string value, string name)
    {
        Value = value;
        Name = name;
    }
        
    public static DistinguishedName Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(value));
        }

        if (!IsDistinguishedName(value))
        {
            throw new InvalidOperationException("Invalid DN format");
        }


        var firstSegment = value.Split(',')[0];
        var name = firstSegment.Split('=')[0];
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Invalid DN format");
        }

        return new(value, name);
    }

    public static bool IsDistinguishedName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(value));
        }

        return value.Contains('=', StringComparison.OrdinalIgnoreCase) &&
               (value.StartsWith("cn", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith("uid", StringComparison.OrdinalIgnoreCase));
    }
}