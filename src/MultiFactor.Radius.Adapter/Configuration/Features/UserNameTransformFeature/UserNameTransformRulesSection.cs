﻿//Copyright(c) 2022 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Configuration.Features.UserNameTransformFeature;
using System.Configuration;

namespace MultiFactor.Radius.Adapter
{
    public class UserNameTransformRulesSection : ConfigurationSection
    {
        [ConfigurationProperty("", IsDefaultCollection = true)]
        public UserNameTransformRulesCollection Members => (UserNameTransformRulesCollection)base[""];
    }
}
