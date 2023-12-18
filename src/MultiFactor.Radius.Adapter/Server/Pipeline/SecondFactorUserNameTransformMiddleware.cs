//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Configuration.Features.UserNameTransformFeature;

namespace MultiFactor.Radius.Adapter.Server.Pipeline
{
    public class SecondFactorUserNameTransformMiddleware : TransformUserNameMiddleware
    {
        protected override UserNameTransformRule[] GetConfigurationRules(RadiusContext context)
        {
            return context.ClientConfiguration.UserNameTransformRules.BeforeSecondFactor;
        }
    }
}
