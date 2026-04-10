using Microsoft.Extensions.DependencyInjection;
using Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.Adapters;
using Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.UseCases.ChangePassword;
using Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.UseCases.FirstFactor;
using Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.UseCases.LoadLdapForest;
using Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.UseCases.LoadProfile;
using Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.UseCases.LoadSchema;
using Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.UseCases.SecondFactor;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.UseCases;

internal static class Module
{
    public static IServiceCollection AddUseCasesInfra(this IServiceCollection services)
    {
        services.AddChangePasswordInfra();
        services.AddCheckConnectionInfra();
        services.AddLdapForestLoadInfra();
        services.AddLoadProfileInfra();
        services.AddLoadSchemaInfra();
        services.AddSecondFactorInfra();
        return services;
    }
}