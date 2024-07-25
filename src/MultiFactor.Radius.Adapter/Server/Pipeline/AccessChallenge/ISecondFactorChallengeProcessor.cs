//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Core.Framework.Context;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Server.Pipeline.AccessChallenge
{
    public interface ISecondFactorChallengeProcessor
    {
        ChallengeIdentifier AddChallengeContext(RadiusContext context);
        bool HasChallengeContext(ChallengeIdentifier identifier);
        Task<ChallengeCode> ProcessChallengeAsync(ChallengeIdentifier identifier, RadiusContext context);
    }

    public enum ChallengeCode
    {
        Accept,
        Reject,
        InProcess
    }
}