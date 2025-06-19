namespace Multifactor.Radius.Adapter.v2.Core.MultifactorApi;

public class ChallengeRequest
{
    public string Identity { get; set; } = string.Empty;
    public string Challenge { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
}