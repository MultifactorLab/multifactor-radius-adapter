using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models.UserNameTransform;
using System.Text.RegularExpressions;

namespace MultiFactor.Radius.Adapter.Core
{
    static class UserNameTransformation
    {
        internal static string Transform(string userName, UserNameTransformRule[] rules)
        {
            foreach (var rule in rules)
            {
                var regex = new Regex(rule.Match);
                if (rule.Count != null)
                {
                    userName = regex.Replace(userName, rule.Replace, rule.Count.Value);
                }
                else
                {
                    userName = regex.Replace(userName, rule.Replace);
                }
            }

            return userName;
        }
    }
}
