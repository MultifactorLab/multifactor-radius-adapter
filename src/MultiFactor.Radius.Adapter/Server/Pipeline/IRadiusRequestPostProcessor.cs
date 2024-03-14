//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using System.Threading.Tasks;
using MultiFactor.Radius.Adapter.Server.Context;

namespace MultiFactor.Radius.Adapter.Server.Pipeline
{
    public interface IRadiusRequestPostProcessor
    {
        Task InvokeAsync(RadiusContext context);
    }
}