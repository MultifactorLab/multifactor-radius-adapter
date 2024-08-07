//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Core.Framework.Context;
using MultiFactor.Radius.Adapter.Core.Framework.Pipeline;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Server.Pipeline.TransformUserName
{
    public class FirstFactorTransformUserNameMiddleware : IRadiusMiddleware
    {
        public Task InvokeAsync(RadiusContext context, RadiusRequestDelegate next)
        {
            UserNameTransformation.Transform(context, context.Configuration.UserNameTransformRules.BeforeFirstFactor);
            return next(context);
        }
    }
}