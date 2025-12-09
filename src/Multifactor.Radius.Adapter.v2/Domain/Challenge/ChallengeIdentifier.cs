using Multifactor.Core.Ldap;

namespace Multifactor.Radius.Adapter.v2.Domain.Challenge;

public class ChallengeIdentifier : ValueObject
{
    public string ClientName { get; }
    public string RequestId { get; }
    public string Value => $"{ClientName}-{RequestId}";


    private ChallengeIdentifier()
    {
        ClientName = string.Empty;
        RequestId = string.Empty;
    }

    public ChallengeIdentifier(string clientName, string requestId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientName);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestId);

        ClientName = clientName;
        RequestId = requestId;
    }

    public override string ToString() => Value;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return ClientName;
        yield return RequestId;
    }
}