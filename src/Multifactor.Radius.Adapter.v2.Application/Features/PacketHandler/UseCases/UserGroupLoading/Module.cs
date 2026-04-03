using Microsoft.Extensions.DependencyInjection;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.UserGroupLoading;

public static class Module
{
    public static IServiceCollection AddUserGroupLoading(this IServiceCollection services)
    {
        return services.AddTransient<UserGroupLoadingStep>();
    }
}