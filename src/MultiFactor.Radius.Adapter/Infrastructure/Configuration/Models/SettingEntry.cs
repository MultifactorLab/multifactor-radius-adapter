//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using FluentValidation;

namespace MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models;

internal class SettingEntry
{
    public string Key { get; set; }
    public string Value { get; set; }
}

internal class SettingEntryValidator : AbstractValidator<SettingEntry>
{
    public SettingEntryValidator()
    {
        RuleFor(x => x.Key).NotNull().NotEmpty();
        RuleFor(x => x.Value).NotNull().NotEmpty();
    }
}
