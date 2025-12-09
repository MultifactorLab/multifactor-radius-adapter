//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Extensions;

internal static class ToPascalCaseExtension
{
    public static string ToPascalCase(this string dashCase)
    {
        if (string.IsNullOrWhiteSpace(dashCase))
            return dashCase;

        return string.Concat(
            dashCase.Split('-')
                .Where(part => !string.IsNullOrEmpty(part))
                .Select(part => char.ToUpperInvariant(part[0]) + part[1..])
        );
    }
}