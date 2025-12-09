using System.Text.RegularExpressions;
using Multifactor.Radius.Adapter.v2.Domain.RadiusAdapter.Sections.UserNameTransform;

namespace Multifactor.Radius.Adapter.v2.Domain;

public static class UserNameTransformation
{
    public static string Transform(string userName, UserNameTransformRule[] rules)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        ArgumentNullException.ThrowIfNull(rules);

        return rules.Where(rule => !string.IsNullOrWhiteSpace(rule.Match)).Aggregate(userName, ApplyRule);
    }

    private static string ApplyRule(string input, UserNameTransformRule rule)
    {
        var regex = new Regex(rule.Match);
        return rule.Count > 0 
            ? regex.Replace(input, rule.Replace, rule.Count)
            : regex.Replace(input, rule.Replace);
    }
}