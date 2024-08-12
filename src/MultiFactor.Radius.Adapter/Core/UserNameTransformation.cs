using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Features.UserNameTransform;
using System.Text.RegularExpressions;

namespace MultiFactor.Radius.Adapter.Core
{
    static class UserNameTransformation
    {
        internal static string Transform(string userName, UserNameTransformRule[] rules)
        {
            foreach (var rule in rules)
            {
                var regex = new Regex(rule.Element.Match);
                if (rule.Element.Count != null)
                {
                    userName = regex.Replace(userName, rule.Element.Replace, rule.Element.Count.Value);
                }
                else
                {
                    userName = regex.Replace(userName, rule.Element.Replace);
                }
            }

            return userName;
        }
    }
}
