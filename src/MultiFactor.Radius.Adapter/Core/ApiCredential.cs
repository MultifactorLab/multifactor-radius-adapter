using System;

namespace MultiFactor.Radius.Adapter.Core
{
    public record ApiCredential
    {
        public string Usr { get; }
        public string Pwd { get; }

        public ApiCredential(string key, string secret)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException($"'{nameof(key)}' cannot be null or whitespace.", nameof(key));
            }

            if (string.IsNullOrWhiteSpace(secret))
            {
                throw new ArgumentException($"'{nameof(secret)}' cannot be null or whitespace.", nameof(secret));
            }

            Usr = key;
            Pwd = secret;
        }
    }
}
