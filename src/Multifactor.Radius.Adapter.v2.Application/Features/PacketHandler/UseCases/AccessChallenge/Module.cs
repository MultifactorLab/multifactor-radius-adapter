using Microsoft.Extensions.DependencyInjection;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.AccessChallenge;

public static class Module
{
    public static IServiceCollection AddAccessChallenge(this IServiceCollection services)
    {
        return services.AddTransient<AccessChallengeStep>();
    }
}