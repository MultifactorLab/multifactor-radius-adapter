﻿//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using FluentValidation;
using MultiFactor.Radius.Adapter.Configuration.Models.RadiusReply;
using MultiFactor.Radius.Adapter.Configuration.Models.UserNameTransform;

namespace MultiFactor.Radius.Adapter.Configuration.Models;

internal class RadiusAdapterConfiguration
{
    public AppSettingsSection AppSettings { get; set; } = new();
    public RadiusReplySection RadiusReply { get; set; } = new();
    public ActiveDirectorySection ActiveDirectory { get; set; } = new();
    public UserNameTransformRulesSection UserNameTransformRules { get; set; } = new();
}

internal class RadiusAdapterConfigurationValidator : AbstractValidator<RadiusAdapterConfiguration>
{
    public RadiusAdapterConfigurationValidator()
    {
        RuleFor(x => x.AppSettings).NotNull();

        RuleFor(x => x.RadiusReply).NotNull();
        RuleFor(x => x.RadiusReply).SetValidator(new RadiusReplySectionValidator());

        RuleFor(x => x.ActiveDirectory).NotNull();

        RuleFor(x => x.UserNameTransformRules).NotNull();
        //RuleFor(x => x.UserNameTransformRules).SetValidator(new UserNameTransformRulesSectionValidator());
    }
}