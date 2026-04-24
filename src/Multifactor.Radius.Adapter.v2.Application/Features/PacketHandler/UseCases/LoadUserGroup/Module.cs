using Microsoft.Extensions.DependencyInjection;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadSchema;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadUserGroup;

public static class Module
{
    public static IServiceCollection AddUserGroupLoading(this IServiceCollection services)
    {
        return services.AddTransient<UserGroupLoadingStep>();
    }
}