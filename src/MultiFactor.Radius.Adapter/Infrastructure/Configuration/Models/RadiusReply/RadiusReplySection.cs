//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using FluentValidation;

namespace MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models.RadiusReply;

public class RadiusReplySection
{
    public RadiusReplyAttributesSection Attributes { get; init; } = new();
}

internal class RadiusReplySectionValidator : AbstractValidator<RadiusReplySection>
{
    public RadiusReplySectionValidator()
    {
        RuleFor(x => x.Attributes).NotNull();
        RuleFor(x => x.Attributes).SetValidator(new RadiusReplyAttributesSectionValidator());
    }
}
