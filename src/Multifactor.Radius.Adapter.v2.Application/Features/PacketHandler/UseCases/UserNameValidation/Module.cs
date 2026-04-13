using Microsoft.Extensions.DependencyInjection;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.UserNameValidation;

public static class Module
{
    public static IServiceCollection AddUserNameValidation(this IServiceCollection services)
    {
        return services.AddTransient<UserNameValidationStep>();
    }
}