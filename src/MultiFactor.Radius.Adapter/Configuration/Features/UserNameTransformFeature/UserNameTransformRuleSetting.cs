using System.Configuration;

namespace MultiFactor.Radius.Adapter.Configuration.Features.UserNameTransformFeature
{
    public class UserNameTransformRuleSetting : ConfigurationElement
    {
        [ConfigurationProperty("", IsDefaultCollection = true)]
        public UserNameTransformRulesCollection Members
        {
            get { return (UserNameTransformRulesCollection)base[""]; }
        }
    }
}
