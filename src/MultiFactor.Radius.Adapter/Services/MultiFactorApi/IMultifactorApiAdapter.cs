//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md


using MultiFactor.Radius.Adapter.Framework.Context;
using MultiFactor.Radius.Adapter.Server;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi.Models;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Services.MultiFactorApi
{
    public interface IMultifactorApiAdapter
    {
        Task<ChallengeResponse> ChallengeAsync(RadiusContext context, string answer, ChallengeIdentifier identifier);
        Task<SecondFactorResponse> CreateSecondFactorRequestAsync(RadiusContext context);
    }
}