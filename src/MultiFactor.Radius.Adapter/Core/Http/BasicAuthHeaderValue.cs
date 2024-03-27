using System;
using System.Text;

namespace MultiFactor.Radius.Adapter.Core.Http
{
    /// <summary>
    /// Represents value (parameter) for a BASIC authentication header. <para />
    /// {username}:{password}
    /// </summary>
    public class BasicAuthHeaderValue
    {
        private readonly string _username;
        private readonly string _password;
        private readonly string _base64;

        public BasicAuthHeaderValue(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException($"'{nameof(username)}' cannot be null or whitespace.", nameof(username));
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException($"'{nameof(password)}' cannot be null or whitespace.", nameof(password));
            }
            _username = username;
            _password = password;
            _base64 = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
        }

        /// <summary>
        /// Returns BASE64 header value representation.
        /// </summary>
        /// <returns></returns>
        public string GetBase64() => _base64;

        public override string ToString() => $"{_username}:{_password}";

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (GetType() != obj.GetType())
            {
                return false;
            }

            var val = (BasicAuthHeaderValue)obj;
            return val._base64 == _base64;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return 17 * _base64.GetHashCode();
            }
        }

        public static bool operator ==(BasicAuthHeaderValue a, BasicAuthHeaderValue b)
        {
            if (a is null && b is null)
            {
                return true;
            }

            if (a is null || b is null)
            {
                return false;
            }

            return a.Equals(b);
        }

        public static bool operator !=(BasicAuthHeaderValue a, BasicAuthHeaderValue b)
        {
            return !(a == b);
        }
    }
}