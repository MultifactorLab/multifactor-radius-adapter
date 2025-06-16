//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Sections.RadiusReply;

public class RadiusReplyAttribute
{
    public string Name { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public string When { get; init; } = string.Empty;
    public string From { get; init; } = string.Empty;
    public bool Sufficient { get; init; }
}
