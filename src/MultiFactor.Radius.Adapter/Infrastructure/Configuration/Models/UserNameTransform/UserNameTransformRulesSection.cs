//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Configuration.Features.UserNameTransformFeature;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Features.UserNameTransformFeature;
using System.Configuration;

namespace MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models.UserNameTransform;

public class UserNameTransformRulesSection : ConfigurationSection
{
    [ConfigurationProperty("", IsDefaultCollection = true)]
    public UserNameTransformRulesCollection Members
    {
        get { return (UserNameTransformRulesCollection)base[""]; }
    }

    [ConfigurationProperty("BeforeFirstFactor")]
    public UserNameTransformRuleSetting BeforeFirstFactor
    {
        get
        {
            var url =
            (UserNameTransformRuleSetting)base["BeforeFirstFactor"];
            return url;
        }
    }

    [ConfigurationProperty("BeforeSecondFactor")]
    public UserNameTransformRuleSetting BeforeSecondFactor
    {
        get
        {
            var url =
            (UserNameTransformRuleSetting)base["BeforeSecondFactor"];
            return url;
        }
    }
}