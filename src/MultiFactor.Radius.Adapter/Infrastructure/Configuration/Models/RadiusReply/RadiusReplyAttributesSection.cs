//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using FluentValidation;
using Microsoft.Extensions.Configuration;
using System;
using System.ComponentModel;

namespace MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models.RadiusReply;

[Description("Attributes")]
public class RadiusReplyAttributesSection
{
    [ConfigurationKeyName("add")]
    private RadiusReplyAttribute[] _elements { get; init; } = Array.Empty<RadiusReplyAttribute>();

    [ConfigurationKeyName("add")]
    private RadiusReplyAttribute _element { get; init; }

    public RadiusReplyAttribute[] Elements
    {
        get
        {
            // To deal with a single element binding to array issue, we should map a single claim manually 
            // See: https://github.com/dotnet/runtime/issues/57325
            if (_elements.Length != 0)
            {
                return _elements;
            }

            if (_element is not null)
            {
                return new [] { _element };
            }

            return Array.Empty<RadiusReplyAttribute>();
        }
    }
}

internal class RadiusReplyAttributesSectionValidator : AbstractValidator<RadiusReplyAttributesSection>
{
    public RadiusReplyAttributesSectionValidator()
    {
        RuleFor(x => x.Elements).NotNull();
        RuleForEach(x => x.Elements).SetValidator(new RadiusReplyAttributeValidator());
    }
}
