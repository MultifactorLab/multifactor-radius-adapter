//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using FluentValidation;

namespace MultiFactor.Radius.Adapter.Configuration.Models.RadiusReply;

internal class RadiusReplyAttribute
{
    public string Name { get; set; }
    public string Value { get; set; }
    public string When { get; set; }
    public string From { get; set; }
    public bool Sufficient { get; set; }
}

internal class RadiusReplyAttributeValidator : AbstractValidator<RadiusReplyAttribute>
{
    private const string _err = "Failed to parse radius reply attribute element: Invalid value of '{PropertyName}' attribute";

    public RadiusReplyAttributeValidator()
    {
        RuleFor(x => x.Name).NotEmpty();

        RuleFor(x => x.Value)
            .Must(x => x != string.Empty)
            .When(x => x is not null)
            .WithMessage(_err);

        RuleFor(x => x.Name)
            .Must(x => x != string.Empty)
            .When(x => x is not null)
            .WithMessage(_err);

        RuleFor(x => x.Name)
            .Must(x => x != string.Empty)
            .When(x => x is not null)
            .WithMessage(_err);
    }
}