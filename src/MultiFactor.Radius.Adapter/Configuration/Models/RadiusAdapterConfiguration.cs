//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using FluentValidation;
using MultiFactor.Radius.Adapter.Configuration.Models.RadiusReply;
using MultiFactor.Radius.Adapter.Configuration.Models.UserNameTransform;

namespace MultiFactor.Radius.Adapter.Configuration.Models;

internal class RadiusAdapterConfiguration
{
    public AppSettingsSection AppSettings { get; init; } = new();
    public RadiusReplySection RadiusReply { get; init; } = new();
    public UserNameTransform.UserNameTransformRulesSection UserNameTransformRules { get; init; } = new();
}

internal class RadiusAdapterConfigurationValidator : AbstractValidator<RadiusAdapterConfiguration>
{
    public RadiusAdapterConfigurationValidator()
    {
        RuleFor(x => x.AppSettings).NotNull();

        RuleFor(x => x.RadiusReply).NotNull();
        RuleFor(x => x.RadiusReply).SetValidator(new RadiusReplySectionValidator());

        RuleFor(x => x.UserNameTransformRules).NotNull();
        RuleFor(x => x.UserNameTransformRules).SetValidator(new UserNameTransformRulesSectionValidator());
    }
}