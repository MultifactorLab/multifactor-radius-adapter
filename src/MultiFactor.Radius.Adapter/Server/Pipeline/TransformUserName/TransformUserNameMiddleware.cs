//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Framework.Context;
using MultiFactor.Radius.Adapter.Framework.Pipeline;
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

        private void ProcessUserNameTransformRules(RadiusContext context)
        {
            var userName = context.UserName;
            if (string.IsNullOrEmpty(userName)) return;

            foreach (var rule in context.ClientConfiguration.UserNameTransformRules)
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

            context.UserName = userName;
        }
    }
}