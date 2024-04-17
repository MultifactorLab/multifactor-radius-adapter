//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Configuration.Features.RadiusReplyAttributeFeature;
using System.Configuration;

//do not change namespace for backward compatibility with older versions
namespace MultiFactor.Radius.Adapter;

public class RadiusReplyAttributesSection : ConfigurationSection
{
    [ConfigurationProperty("Attributes")]
    public RadiusReplyAttributesCollection Members => (RadiusReplyAttributesCollection)this["Attributes"];
}
