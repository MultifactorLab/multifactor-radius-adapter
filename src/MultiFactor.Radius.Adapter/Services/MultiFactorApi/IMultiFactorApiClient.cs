//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md


using MultiFactor.Radius.Adapter.Infrastructure.Http;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi.Dto;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Services.MultiFactorApi
{
    public interface IMultifactorApiClient
    {
        Task<AccessRequestDto> CreateRequestAsync(CreateRequestDto dto, BasicAuthHeaderValue auth);
        Task<AccessRequestDto> ChallengeAsync(ChallengeDto dto, BasicAuthHeaderValue auth);
    }
}