//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

namespace MultiFactor.Radius.Adapter.Configuration.ConfigurationLoading
{
    public interface IClientConfigurationsProvider
    {
        System.Configuration.Configuration[] GetClientConfigurations();
    }
}