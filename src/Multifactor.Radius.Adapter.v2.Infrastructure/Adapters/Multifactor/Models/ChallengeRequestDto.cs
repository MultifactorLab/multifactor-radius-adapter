using Multifactor.Radius.Adapter.v2.Application.Features.Multifactor.Models;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Adapters.Multifactor.Models;

public class ChallengeRequestDto
{
    public string Identity { get; set; } = string.Empty;
    public string Challenge { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    
    public static ChallengeRequestDto FromQuery(ChallengeRequestQuery query)
    {
        return new ChallengeRequestDto
        {
            Identity = query.Identity,
            Challenge = query.Challenge,
            RequestId = query.RequestId
        };
    }
}