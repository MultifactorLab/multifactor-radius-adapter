using System.DirectoryServices.Protocols;
using System.Text;
using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.Ldap.Identity;

namespace Multifactor.Radius.Adapter.v2.Core
{
    public static class Utils
    {
        /// <summary>
        /// Convert a string of hex encoded bytes to a byte array
        /// </summary>
        public static byte[] StringToByteArray(string hex)
        {
            var NumberChars = hex.Length;
            var bytes = new byte[NumberChars / 2];
            for (var i = 0; i < NumberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }

            return bytes;
        }


        /// <summary>
        /// Convert a byte array to a string of hex encoded bytes
        /// </summary>
        public static string ToHexString(this byte[] bytes)
        {
            return bytes != null ? BitConverter.ToString(bytes).ToLowerInvariant().Replace("-", "") : null;
        }

        /// <summary>
        /// Base64 encoded string
        /// </summary>
        public static string Base64(this byte[] bytes)
        {
            if (bytes != null)
                return Convert.ToBase64String(bytes);

            return null;
        }

        /// <summary>
        /// Converts string from base64 to utf-8 
        /// </summary>
        /// <param name="st"></param>
        /// <returns></returns>
        public static string Base64toUtf8(this string st)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(st));
        }

        /// <summary>
        /// User name without domain
        /// </summary>
        public static string CanonicalizeUserName(string? userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
                return string.Empty;

            var identity = userName.ToLower();

            var index = identity.IndexOf('\\', StringComparison.Ordinal);
            if (index > 0)
                identity = identity[(index + 1)..];

            index = identity.IndexOf('@', StringComparison.Ordinal);
            if (index > 0)
                identity = identity[..index];

            return identity;
        }

        /// <summary>
        /// Check if username does not contains domain prefix or suffix
        /// </summary>
        public static bool IsCanicalUserName(string? userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
                return true;

            return userName.IndexOfAny(['\\', '@']) == -1;
        }

        public static string[] SplitString(string? target, string separator = ";") => target
            ?.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];

        public static LdapConnectionOptions CreateLdapConnectionOptions(ILdapServerConfiguration serverConfiguration) =>
            new(new LdapConnectionString(serverConfiguration.ConnectionString),
                AuthType.Basic,
                serverConfiguration.UserName,
                serverConfiguration.Password,
                TimeSpan.FromSeconds(serverConfiguration.BindTimeoutInSeconds));

        public static string GetUpnSuffix(UserIdentity userIdentity)
        {
            if (userIdentity.Format != UserIdentityFormat.UserPrincipalName)
                return string.Empty;

            var suffix = userIdentity.Identity.Split('@', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Last();
            return suffix;
        }
    }
}