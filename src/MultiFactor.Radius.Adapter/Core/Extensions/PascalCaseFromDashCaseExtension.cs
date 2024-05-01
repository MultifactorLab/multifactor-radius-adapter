//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using System;
using System.Linq;

namespace MultiFactor.Radius.Adapter.Core.Extensions;

internal static class PascalCaseFromDashCaseExtension
{
    public static string ToPascalCaseFromDashCase(this string dashCase)
    {
        if (string.IsNullOrWhiteSpace(dashCase))
        {
            throw new ArgumentException($"'{nameof(dashCase)}' cannot be null or whitespace.", nameof(dashCase));
        }

        var splitted = dashCase.Split(new[] { '-' });
        var upperFirstChar = splitted.Select(x => $"{char.ToUpperInvariant(x[0])}{x[1..]}");
        var pascalCase = string.Join(string.Empty, upperFirstChar);

        return pascalCase;
    }
}
