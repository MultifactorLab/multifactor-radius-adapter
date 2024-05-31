//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

namespace MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models.RadiusReply;

public class RadiusReplyAttribute
{
    public string Name { get; init; }
    public string Value { get; init; }
    public string When { get; init; }
    public string From { get; init; }
    public bool Sufficient { get; init; }
}
