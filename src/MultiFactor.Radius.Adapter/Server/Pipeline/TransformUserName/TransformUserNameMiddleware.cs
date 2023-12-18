//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Core.Framework.Context;
using MultiFactor.Radius.Adapter.Core.Framework.Pipeline;
using MultiFactor.Radius.Adapter.Configuration.Features.UserNameTransformFeature;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Server.Pipeline.TransformUserName
{
    public class TransformUserNameMiddleware : IRadiusMiddleware
    {
        public async Task InvokeAsync(RadiusContext context, RadiusRequestDelegate next)
        {
            ProcessUserNameTransformRules(context);
            await next(context);
        }

        protected virtual UserNameTransformRule[] GetConfigurationRules(RadiusContext context)
        {
            return context.Configuration.UserNameTransformRules.BeforeFirstFactor;
        }
    
        private void ProcessUserNameTransformRules(RadiusContext context)
        {
            if (string.IsNullOrEmpty(context.OriginalUserName)) return;
            var userName = context.OriginalUserName;

            foreach (var rule in GetConfigurationRules(context))
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