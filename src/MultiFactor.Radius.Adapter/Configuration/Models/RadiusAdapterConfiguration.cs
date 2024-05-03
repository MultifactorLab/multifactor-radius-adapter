//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using System.Configuration;
using FluentValidation;
using MultiFactor.Radius.Adapter.Configuration.Models.RadiusReply;

namespace MultiFactor.Radius.Adapter.Configuration.Models;

internal class RadiusAdapterConfiguration
{
    public AppSettingsSection AppSettings { get; set; } = new();
    public RadiusReplySection RadiusReply { get; set; } = new();
    public ActiveDirectorySection ActiveDirectory { get; set; } = new();
}

internal class RadiusAdapterConfigurationValidator : AbstractValidator<RadiusAdapterConfiguration>
{
    public RadiusAdapterConfigurationValidator()
    {
        RuleFor(x => x.AppSettings).NotNull();

        RuleFor(x => x.RadiusReply).NotNull();
        RuleFor(x => x.RadiusReply).SetValidator(new RadiusReplySectionValidator());

        RuleFor(x => x.ActiveDirectory).NotNull();
    }
}