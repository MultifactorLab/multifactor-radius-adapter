using Microsoft.Extensions.DependencyInjection;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.AccessChallenge;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.AccessGroupsCheck;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.AccessRequestFilter;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.FirstFactor;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.IpWhiteList;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadLdapForest;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadProfile;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadSchema;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.PreAuthCheck;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.PreAuthPostCheck;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SecondFactor;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SharedServices;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.StatusServerFilter;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.UserGroupLoading;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.UserNameValidation;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases;

public static class Module
{
    public static IServiceCollection AddUseCases(this IServiceCollection services)
    {
        return services
            .AddSharedServices()
            .AddUserNameValidation()
            .AddUserGroupLoading()
            .AddStatusServerFilter()
            .AddSecondFactor()
            .AddPreAuthPostCheck()
            .AddLoadLdapSchema()
            .AddLoadLdapForest()
            .AddIpWhiteList()
            .AddFirstFactor()
            .AddAccessRequestFiltering()
            .AddAccessGroupsCheck()
            .AddAccessChallenge()
            .AddPreAuthCheck()
            .AddProfileLoading();
    }
}