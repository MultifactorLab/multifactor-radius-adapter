//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using System.Configuration;

namespace MultiFactor.Radius.Adapter.Infrastructure.Configuration.Features.RadiusReplyAttributeFeature
{
    public class RadiusReplyAttributeElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsKey = false, IsRequired = true)]
        public string Name => (string)this["name"];

        [ConfigurationProperty("value", IsKey = false, IsRequired = false)]
        public string Value => (string)this["value"];

        [ConfigurationProperty("when", IsKey = false, IsRequired = false)]
        public string When => (string)this["when"];

        [ConfigurationProperty("from", IsKey = false, IsRequired = false)]
        public string From => (string)this["from"];

        [ConfigurationProperty("sufficient", IsKey = false, IsRequired = false)]
        public bool Sufficient => (bool)this["sufficient"];
    }
}
