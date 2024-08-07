//Copyright(c) 2022 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

namespace MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models.UserNameTransformFeature;

public class UserNameTransformRulesElement
{
    public string Match { get; init; }
    public string Replace { get; init; }
    public int? Count { get; init; }
}
