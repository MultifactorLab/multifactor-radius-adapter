//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using Microsoft.Extensions.Configuration;
using System.ComponentModel;
using System;
using FluentValidation;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace MultiFactor.Radius.Adapter.Configuration.Models;

[Description("appSettings")]
internal class AppSettingsSection
{
    [ConfigurationKeyName("add")]
    public SettingEntry[] Entries { get; set; } = Array.Empty<SettingEntry>();
}

internal class AppSettingsSectionValidator : AbstractValidator<AppSettingsSection>
{
    public AppSettingsSectionValidator()
    {
        RuleFor(x => x.Entries).NotNull();
        RuleForEach(x => x.Entries).SetValidator(new SettingEntryValidator());
        RuleFor(x => x.Entries).Must(x =>
        {
            var duplicate = FirstDuplicateOrNull(x.Select(e => e.Key));
            return duplicate == null;
        }).WithMessage((root, current) =>
        {
            var duplicate = FirstDuplicateOrNull(current.Select(e => e.Key));
            var sectionName = GetDescriptionOrNull(root) ?? root.GetType().Name;
            return $"Duplicate key '{duplicate}' found in <{sectionName}> section";
        });
    }

    private static T FirstDuplicateOrNull<T>(IEnumerable<T> arr)
    {
        var hashset = new HashSet<T>();
        return arr.FirstOrDefault(x => !hashset.Add(x));
    }

    private static string GetDescriptionOrNull(object target)
    {
        return target?.GetType()
            .GetCustomAttribute<DescriptionAttribute>()
            ?.Description;
    }
}
