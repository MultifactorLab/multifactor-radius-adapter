using System;
using System.Text;

namespace MultiFactor.Radius.Adapter.Core
{
    public class MultifactorApiCredential
    {
        private readonly string _key;
        private readonly string _secret;

        public MultifactorApiCredential(string key, string secret)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException($"'{nameof(key)}' cannot be null or whitespace.", nameof(key));
            }

            if (string.IsNullOrWhiteSpace(secret))
            {
                throw new ArgumentException($"'{nameof(secret)}' cannot be null or whitespace.", nameof(secret));
            }

            _key = key;
            _secret = secret;
        }

        public string GetHttpBasicAuthorizationHeaderValue()
        {
            return Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_key}:{_secret}"));
        }
    }
}
