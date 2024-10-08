﻿//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

namespace MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models.UserNameTransform;

public class UserNameTransformRulesSection : UserNameTransformRulesCollection
{
    public UserNameTransformRulesCollection BeforeFirstFactor { get; init; } = new();
    public UserNameTransformRulesCollection BeforeSecondFactor { get; init; } = new();
}