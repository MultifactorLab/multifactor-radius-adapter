using MultiFactor.Radius.Adapter.Framework.Context;
using MultiFactor.Radius.Adapter.Framework.Pipeline;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Server.Pipeline.PreSecondFactorAuthentication
{
    public class PreSecondFactorAuthenticationMiddleware : IRadiusMiddleware
    {
        public Task InvokeAsync(RadiusContext context, RadiusRequestDelegate next)
        {
            return next(context);
        }
    }
}
