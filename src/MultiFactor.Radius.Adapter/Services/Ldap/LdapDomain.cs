using MultiFactor.Radius.Adapter.Core.Services.Ldap;
using System;

namespace MultiFactor.Radius.Adapter.Services.Ldap
{
    public class LdapUser
    {
        private readonly string _original;

        public IdentityType Type { get; }
        public string Name { get; }
        public string Prefix { get; }


        private LdapUser(string original, IdentityType type, string name, string prefix)
        {
            _original = original;
            Type = type;
            Name = name;
            Prefix = prefix;
        }

        public static LdapUser Parse(string identity)
        {
            if (string.IsNullOrWhiteSpace(identity)) throw new ArgumentException($"'{nameof(identity)}' cannot be null or whitespace.", nameof(identity));

            var trimmed = identity.Trim();
            var (prefix, name) = Separate(trimmed);
            var lwrName = name.ToLower();

            if (prefix != string.Empty)
            {
                return new LdapUser(FormatOriginal(prefix, lwrName), IdentityType.Uid, lwrName, prefix);
            }

            if (lwrName.Contains('='))
            {
                return new LdapUser(lwrName, IdentityType.DistinguishedName, lwrName, prefix);
            }

            if (lwrName.Contains('@'))
            {
                return new LdapUser(lwrName, IdentityType.UserPrincipalName, lwrName, prefix);
            }

            return new LdapUser(lwrName, IdentityType.Uid, lwrName, prefix);
        }

        private static (string prefix, string name) Separate(string identity)
        {
            var index = identity.IndexOf("\\");
            if (index == -1) return (string.Empty, identity);
            if (index == 0) throw new ArgumentException("Incorrect identity");

            return (identity.Substring(0, index), identity.Substring(index + 1));
        }

        private static string FormatOriginal(string prefix, string name)
        {
            return prefix == string.Empty ? name : $"{prefix}\\{name}";
        }

        public override string ToString() => _original;     
    }

    public class LdapDomain
    {
        private readonly LdapIdentity _identity;

        public string Name => _identity.Name;

        private LdapDomain(LdapIdentity identity)
        {
            _identity = identity;
        }

        public static LdapDomain Parse(string defaultNamingContext)
        {
            if (string.IsNullOrWhiteSpace(defaultNamingContext))
            {
                throw new ArgumentException($"'{nameof(defaultNamingContext)}' cannot be null or whitespace.", nameof(defaultNamingContext));
            }

            return new LdapDomain(new LdapIdentity(defaultNamingContext, IdentityType.DistinguishedName));
        }
    }
}
