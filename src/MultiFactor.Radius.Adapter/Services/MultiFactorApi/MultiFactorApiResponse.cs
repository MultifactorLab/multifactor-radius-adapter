//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

namespace MultiFactor.Radius.Adapter.Services.MultiFactorApi
{
    public class MultiFactorApiResponse<TModel>
    {
        public bool Success { get; set; }
        public TModel Model { get; set; }
    }
}
