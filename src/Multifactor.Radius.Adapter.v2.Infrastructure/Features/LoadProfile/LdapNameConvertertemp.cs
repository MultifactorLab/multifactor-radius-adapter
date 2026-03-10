namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.LoadProfile;

using System.Text.RegularExpressions;

public static class LdapNameConverter //TODO нахуй вырезать 
{
    public static string ConvertToUpn(string username)
    {
        if (string.IsNullOrEmpty(username))
            throw new ArgumentNullException(nameof(username));

        if (username.Contains("@") && Regex.IsMatch(username, @"^[^@]+@[^@]+\.[^@]+$"))
            return username;
        if (username.Contains("=") && (username.Contains("CN=") || username.Contains("DC=")))
        {
            string domain = ExtractDomainFromDN(username);
            string cn = ExtractCN(username);
            return $"{cn}@{domain}";
        }
        return username;
    }
    private static string ExtractDomainFromDN(string distinguishedName)
    {
        var dcParts = new List<string>();
        var parts = distinguishedName.Split(',');

        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (trimmed.StartsWith("DC=", StringComparison.OrdinalIgnoreCase))
                dcParts.Add(trimmed.Substring(3));
        }

        return string.Join(".", dcParts);
    }

    private static string ExtractCN(string distinguishedName)
    {
        var parts = distinguishedName.Split(',');
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (trimmed.StartsWith("CN=", StringComparison.OrdinalIgnoreCase))
                return trimmed.Substring(3);
        }
        return string.Empty;
    }
}
