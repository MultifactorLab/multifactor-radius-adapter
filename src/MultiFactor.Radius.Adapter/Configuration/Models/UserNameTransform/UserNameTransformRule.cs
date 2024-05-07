//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using FluentValidation;

namespace MultiFactor.Radius.Adapter.Configuration.Models.UserNameTransform;

internal class UserNameTransformRule
{
    public string Match { get; set; }
    public string Replace { get; set; }
    public int? Count { get; set; }
}

internal class UserNameTransformRuleValidator : AbstractValidator<UserNameTransformRule>
{
    public UserNameTransformRuleValidator()
    {
        RuleFor(x => x.Match).NotEmpty();
        RuleFor(x => x.Replace).NotEmpty();
    }
}