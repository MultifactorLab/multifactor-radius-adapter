//Copyright(c) 2022 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using System.Configuration;

namespace MultiFactor.Radius.Adapter.Configuration.Features.UserNameTransformFeature
{
    public class UserNameTransformRulesElement : ConfigurationElement
    {
        [ConfigurationProperty("match", IsKey = false, IsRequired = true)]
        public string Match => (string)this["match"];

        [ConfigurationProperty("replace", IsKey = false, IsRequired = true)]
        public string Replace => (string)this["replace"];

        [ConfigurationProperty("count", IsKey = false, IsRequired = false)]
        public int? Count => (int?)this["count"];
    }
}
