using System.Text.RegularExpressions;
using Multifactor.Core.Ldap.LangFeatures;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Sections.UserNameTransform;

namespace Multifactor.Radius.Adapter.v2.Core;

public class UserNameTransformation
{
    internal static string Transform(string userName, UserNameTransformRule[] rules)
    {
        Throw.IfNullOrWhiteSpace(userName, nameof(userName));
        Throw.IfNull(rules, nameof(rules));

        foreach (var rule in rules)
        {
            if (string.IsNullOrWhiteSpace(rule.Match))
                continue;

            var regex = new Regex(rule.Match);
            if (rule.Count > 0)
            {
                userName = regex.Replace(userName, rule.Replace, rule.Count);
            }
            else
            {
                userName = regex.Replace(userName, rule.Replace);
            }
        }

        return userName;
    }
}