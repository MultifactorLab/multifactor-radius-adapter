//Copyright(c) 2022 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Sections.UserNameTransform;

public class UserNameTransformRule
{
    public string? Match { get; init; }
    public string? Replace { get; init; }
    public int? Count { get; init; }
}
