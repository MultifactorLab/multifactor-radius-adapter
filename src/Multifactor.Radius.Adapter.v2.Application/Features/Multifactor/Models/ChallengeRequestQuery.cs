namespace Multifactor.Radius.Adapter.v2.Application.Features.Multifactor.Models;

public class ChallengeRequestQuery
{
    public string Identity { get; set; }
    public string Challenge { get; set; }
    public string RequestId { get; set; }
}