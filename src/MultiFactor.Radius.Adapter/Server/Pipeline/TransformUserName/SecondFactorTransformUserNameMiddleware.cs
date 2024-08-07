//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Core.Framework.Context;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models.UserNameTransform;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models.UserNameTransformFeature;

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
