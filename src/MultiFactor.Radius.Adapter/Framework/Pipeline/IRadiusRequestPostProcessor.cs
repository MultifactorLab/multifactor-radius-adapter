//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using System.Threading.Tasks;
using MultiFactor.Radius.Adapter.Framework.Context;

namespace MultiFactor.Radius.Adapter.Framework.Pipeline;

public interface IRadiusRequestPostProcessor
{
    Task InvokeAsync(RadiusContext context);
}