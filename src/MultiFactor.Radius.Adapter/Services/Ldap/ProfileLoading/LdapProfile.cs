using System.Collections.Generic;
using System.Runtime.InteropServices;
using MultiFactor.Radius.Adapter.Core.Ldap;

namespace MultiFactor.Radius.Adapter.Services.Ldap.ProfileLoading
{
    public class LdapProfile : ILdapProfile, ILdapProfileBuilder
    {
        public LdapIdentity BaseDn { get; set; }
        public string DistinguishedName { get; set; }
        public string DistinguishedNameEscaped => EscapeDn(DistinguishedName);

        public string Upn { get; private set; }
        public string DisplayName { get; private set; }
        public string Email { get; private set; }
        public string Phone { get; private set; }

        private readonly List<string> _memberOf = new();
        public string[] MemberOf => _memberOf.ToArray();

        private readonly Dictionary<string, object> _ldapAttrs = new();
        public IReadOnlyDictionary<string, object> LdapAttrs => _ldapAttrs;

        private LdapProfile(LdapIdentity baseDn, string dn)
        {
            if (string.IsNullOrWhiteSpace(dn))
            {
                throw new System.ArgumentException($"'{nameof(dn)}' cannot be null or whitespace.", nameof(dn));
            }

            BaseDn = baseDn ?? throw new System.ArgumentNullException(nameof(baseDn));
            DistinguishedName = dn;
        }

        public static ILdapProfileBuilder CreateBuilder(LdapIdentity baseDn, string distinguishedName)
        {
            return new LdapProfile(baseDn, distinguishedName);
        }

        private static string EscapeDn(string dn)
        {
            var ret = dn
                .Replace("(", @"\28")
                .Replace(")", @"\29");

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ret = ret.Replace("\"", "\\\""); //quotes
                ret = ret.Replace("\\,", "\\5C,"); //comma
            }

            return ret;
        }

        public ILdapProfile Build()
        {
            return this;
        }

        public ILdapProfileBuilder AddMemberOf(string group)
        {
            if (string.IsNullOrWhiteSpace(group))
            {
                throw new System.ArgumentException($"'{nameof(group)}' cannot be null or whitespace.", nameof(group));
            }

            if (!_memberOf.Contains(group))
            {
                _memberOf.Add(group);
            }

            return this;
        }

        public ILdapProfileBuilder SetUpn(string upn)
        {
            if (string.IsNullOrWhiteSpace(upn))
            {
                throw new System.ArgumentException($"'{nameof(upn)}' cannot be null or whitespace.", nameof(upn));
            }

            Upn = upn;
            return this;
        }

        public ILdapProfileBuilder SetDisplayName(string displayname)
        {
            if (string.IsNullOrWhiteSpace(displayname))
            {
                throw new System.ArgumentException($"'{nameof(displayname)}' cannot be null or whitespace.", nameof(displayname));
            }

            DisplayName = displayname;
            return this;
        }

        public ILdapProfileBuilder SetEmail(string email)
        {
            Email = email ?? throw new System.ArgumentNullException(nameof(email));
            return this;
        }

        public ILdapProfileBuilder SetPhone(string phone)
        {
            Phone = phone ?? throw new System.ArgumentNullException(nameof(phone));
            return this;
        }

        public ILdapProfileBuilder AddLdapAttr(string attr, object value)
        {
            if (string.IsNullOrWhiteSpace(attr))
            {
                throw new System.ArgumentException($"'{nameof(attr)}' cannot be null or whitespace.", nameof(attr));
            }

            _ldapAttrs.Add(attr, value);
            return this;
        }
    }
}
