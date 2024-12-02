using System.Threading.Tasks;
using MultiFactor.Radius.Adapter.Core.Framework.Context;

namespace MultiFactor.Radius.Adapter.Server.Pipeline.AccessChallenge;

public interface IChallengeProcessor
{
    ChallengeIdentifier AddChallengeContext(RadiusContext context);
    bool HasChallengeContext(ChallengeIdentifier identifier);
    Task<ChallengeCode> ProcessChallengeAsync(ChallengeIdentifier identifier, RadiusContext context);
    public ChallengeType ChallengeType { get; }
}

public enum ChallengeCode
{
    Accept,
    Reject,
    InProcess
}

public enum ChallengeType
{
    None = 0,
    SecondFactor,
    PasswordChange
}