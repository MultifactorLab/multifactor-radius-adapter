//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using FluentValidation;
using Microsoft.Extensions.Configuration;
using System;
using System.ComponentModel;

namespace MultiFactor.Radius.Adapter.Configuration.Models.RadiusReply;

[Description("Attributes")]
internal class RadiusReplyAttributesSection
{
    [ConfigurationKeyName("add")]
    public RadiusReplyAttribute[] Elements { get; set; } = Array.Empty<RadiusReplyAttribute>();
}

internal class RadiusReplyAttributesSectionValidator : AbstractValidator<RadiusReplyAttributesSection>
{
    public RadiusReplyAttributesSectionValidator()
    {
        RuleFor(x => x.Elements).NotNull();
        RuleForEach(x => x.Elements).SetValidator(new RadiusReplyAttributeValidator());
    }
}
