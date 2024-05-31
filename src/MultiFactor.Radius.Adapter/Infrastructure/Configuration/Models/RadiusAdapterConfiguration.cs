//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models.RadiusReply;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models.UserNameTransform;

namespace MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models;

public class RadiusAdapterConfiguration
{
    public AppSettingsSection AppSettings { get; init; } = new();
    public RadiusReplySection RadiusReply { get; init; } = new();
    public UserNameTransformRulesSection UserNameTransformRules { get; init; } = new();
}
