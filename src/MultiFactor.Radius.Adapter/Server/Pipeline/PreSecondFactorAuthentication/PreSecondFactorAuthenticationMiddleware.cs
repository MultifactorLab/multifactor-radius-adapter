using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Framework.Context;
using MultiFactor.Radius.Adapter.Framework.Pipeline;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Server.Pipeline.PreSecondFactorAuthentication
{
    public class PreSecondFactorAuthenticationMiddleware : IRadiusMiddleware
    {
        private readonly ILogger<PreSecondFactorAuthenticationMiddleware> _logger;

        public PreSecondFactorAuthenticationMiddleware(ILogger<PreSecondFactorAuthenticationMiddleware> logger)
        {
            _logger = logger;
        }

        public async Task InvokeAsync(RadiusContext context, RadiusRequestDelegate next)
        {
            if (context.Authentication.SecondFactor != AuthenticationCode.Awaiting)
            {
                await next(context);
                return;
            }

            if (context.Bypass2Fa)
            {
                // second factor not required
                _logger.LogInformation("Bypass second factor for user '{user:l}'", context.UserName);

                context.ResponseCode = PacketCode.AccessAccept;
                context.Authentication.SetSecondFactor(AuthenticationCode.Bypass);

                await next(context);
                return;
            }

            await next(context);
        }
    }
}
