//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using Config = System.Configuration.Configuration;

namespace MultiFactor.Radius.Adapter.Configuration.Core;

public interface IClientConfigurationsProvider
{
    Config[] GetClientConfigurations();
}