//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md


using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Server;
using MultiFactor.Radius.Adapter.Server.Context;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Services.MultiFactorApi
{
    public interface IMultiFactorApiClient
    {
        Task<PacketCode> Challenge(RadiusContext context, string answer, ChallengeRequestIdentifier identifier);
        Task<PacketCode> CreateSecondFactorRequest(RadiusContext context);
    }
}