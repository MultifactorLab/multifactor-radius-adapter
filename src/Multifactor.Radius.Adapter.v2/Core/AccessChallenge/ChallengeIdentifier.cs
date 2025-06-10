namespace Multifactor.Radius.Adapter.v2.Core.AccessChallenge;

public class ChallengeIdentifier
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

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (obj == this) return true;

        if (obj is not ChallengeIdentifier other) return false;
        var isEmpty = string.IsNullOrWhiteSpace(_identifier);
        return !isEmpty && _identifier == other._identifier;
    }

    public override int GetHashCode()
    {
        return 23 + _identifier.GetHashCode();
    }
}