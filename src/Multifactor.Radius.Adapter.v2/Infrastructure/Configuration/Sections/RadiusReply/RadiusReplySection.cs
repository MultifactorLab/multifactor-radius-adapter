//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.Sections.RadiusReply;

public class RadiusReplySection
{
    public RadiusReplyAttributesSection Attributes { get; init; } = new();
}
