//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Framework.Context;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Server.Pipeline.AccessChallenge
{
    public interface IChallengeProcessor
    {
        void AddState(ChallengeRequestIdentifier identifier, RadiusContext context);
        bool HasState(ChallengeRequestIdentifier identifier);
        Task<PacketCode> ProcessChallengeAsync(ChallengeRequestIdentifier identifier, RadiusContext context);
    }
}