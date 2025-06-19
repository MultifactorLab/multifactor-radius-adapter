using Multifactor.Core.Ldap;

namespace Multifactor.Radius.Adapter.v2.Core.AccessChallenge;

public class ChallengeIdentifier : ValueObject
{
    private readonly string _identifier;

    public string RequestId { get; }

    public static ChallengeIdentifier Empty => new();

    private ChallengeIdentifier()
    {
        _identifier = string.Empty;
        RequestId = string.Empty;
    }

    public ChallengeIdentifier(string clientName, string requestId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientName, nameof(clientName));
        ArgumentException.ThrowIfNullOrWhiteSpace(requestId, nameof(requestId));

        _identifier = $"{clientName}-{requestId}";
        RequestId = requestId;
    }

    public override string ToString()
    {
        return _identifier;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return _identifier;
    }
}