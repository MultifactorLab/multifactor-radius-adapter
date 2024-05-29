//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using FluentValidation;
using Microsoft.Extensions.Configuration;
using System;

namespace MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models.UserNameTransform;

public class UserNameTransformRulesSection
{
    [ConfigurationKeyName("add")]
    public UserNameTransformRule[] Elements { get; set; } = Array.Empty<UserNameTransformRule>();
}

internal class UserNameTransformRulesSectionValidator : AbstractValidator<UserNameTransformRulesSection>
{
    public UserNameTransformRulesSectionValidator()
    {
        RuleFor(x => x.Elements).NotNull();
        RuleForEach(x => x.Elements).SetValidator(new UserNameTransformRuleValidator());
    }
}