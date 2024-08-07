using MultiFactor.Radius.Adapter.Core.Framework.Context;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Features.UserNameTransform;
using System.Text.RegularExpressions;

namespace MultiFactor.Radius.Adapter.Server.Pipeline.TransformUserName
{
    static class UserNameTransformation
    {
        internal static void Transform(RadiusContext context, UserNameTransformRule[] rules)
        {
            if (string.IsNullOrEmpty(context.OriginalUserName)) return;
            var userName = context.OriginalUserName;

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

            context.TransformRadiusRequestAttribute("User-Name", userName);
        }
    }
}
