using MultiFactor.Radius.Adapter.Core.Services.Ldap;
using System;

namespace MultiFactor.Radius.Adapter.Services.Ldap
{
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
