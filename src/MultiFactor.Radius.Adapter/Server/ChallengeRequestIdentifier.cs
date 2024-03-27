//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Configuration.Core;
using System;

namespace MultiFactor.Radius.Adapter.Server;

public class ChallengeRequestIdentifier
{
    private readonly string _identifier;

    public string RequestId { get; }

    public static ChallengeRequestIdentifier Empty => new ChallengeRequestIdentifier();

    private ChallengeRequestIdentifier()
    {
        _identifier = null;
        RequestId = null;
    }

    public ChallengeRequestIdentifier(string clientName, string requestId)
    {
        if (string.IsNullOrWhiteSpace(clientName))
        {
            throw new ArgumentException($"'{nameof(clientName)}' cannot be null or whitespace.", nameof(clientName));
        }

        if (string.IsNullOrWhiteSpace(requestId))
        {
            throw new ArgumentException($"'{nameof(requestId)}' cannot be null or whitespace.", nameof(requestId));
        }

        _identifier = $"{clientName}-{requestId}";
        RequestId = requestId;
    }

    public override string ToString()
    {
        return _identifier;
    }

    public override bool Equals(object obj)
    {
        if (obj is null) return false;
        if (obj == this) return true;

        var other = obj as ChallengeRequestIdentifier;
        if (other == null) return false;

        return _identifier != null && _identifier == other._identifier;
    }

    public override int GetHashCode()
    {
        return 23 + _identifier.GetHashCode();
    }
}