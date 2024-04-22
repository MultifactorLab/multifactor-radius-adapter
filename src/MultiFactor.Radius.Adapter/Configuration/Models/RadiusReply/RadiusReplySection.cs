//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using FluentValidation;
using System.ComponentModel;

namespace MultiFactor.Radius.Adapter.Configuration.Models.RadiusReply;

[Description("RadiusReply")]
internal class RadiusReplySection
{
    public RadiusReplyAttributesSection Attributes { get; set; } = new();
}

internal class RadiusReplySectionValidator : AbstractValidator<RadiusReplySection>
{
    public RadiusReplySectionValidator()
    {
        RuleFor(x => x.Attributes).NotNull();
        RuleFor(x => x.Attributes).SetValidator(new RadiusReplyAttributesSectionValidator());
    }
}