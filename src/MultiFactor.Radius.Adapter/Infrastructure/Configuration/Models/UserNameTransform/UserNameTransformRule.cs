//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using FluentValidation;

namespace MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models.UserNameTransform;

public class UserNameTransformRule
{
    public string Match { get; init; }
    public string Replace { get; init; }
    public int? Count { get; init; }
}

internal class UserNameTransformRuleValidator : AbstractValidator<UserNameTransformRule>
{
    public UserNameTransformRuleValidator()
    {
        RuleFor(x => x.Match).NotEmpty();
        RuleFor(x => x.Replace).NotEmpty();
    }
}