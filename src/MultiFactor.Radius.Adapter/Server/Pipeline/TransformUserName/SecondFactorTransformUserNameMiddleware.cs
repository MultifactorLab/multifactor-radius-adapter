//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Configuration.Features.UserNameTransformFeature;
using MultiFactor.Radius.Adapter.Framework.Context;

namespace MultiFactor.Radius.Adapter.Server.Pipeline.TransformUserName
{
    public class SecondFactorTransformUserNameMiddleware : TransformUserNameMiddleware
    {
        protected override UserNameTransformRule[] GetConfigurationRules(RadiusContext context)
        {
            return context.Configuration.UserNameTransformRules.BeforeSecondFactor;
        }
    }
}
