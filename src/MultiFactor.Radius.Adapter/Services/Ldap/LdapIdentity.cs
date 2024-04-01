using MultiFactor.Radius.Adapter.Core.Services.Ldap;
using System;
using System.Linq;

namespace MultiFactor.Radius.Adapter.Services.Ldap
{
    public class LdapIdentity
    {
        public string Name { get; private set; }
        public IdentityType Type { get; private set; }

        public LdapIdentity (string name, IdentityType type)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException($"'{nameof(name)}' cannot be null or whitespace.", nameof(name));
            }

            Name = name;
            Type = type;
        }

        public static LdapIdentity ParseUser(string name)
        {
            return Parse(name, true);
        }
        
        public static LdapIdentity ParseGroup(string name)
        {
            return Parse(name, false);
        }
        
        /// <summary>
        /// Converts domain.local to DC=domain,DC=local
        /// </summary>
        public static LdapIdentity FqdnToDn(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            var portIndex = name.IndexOf(":");
            if (portIndex > 0)
            {
                name = name.Substring(0, portIndex);
            }

            var domains = name.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            var dn = domains.Select(p => $"DC={p}").ToArray();

            return new LdapIdentity(string.Join(",", dn), IdentityType.DistinguishedName);
        }
        
        /// <summary>
        /// DC part from DN
        /// </summary>
        public static LdapIdentity BaseDn(string dn)
        {
            var ncs = dn.Split(new[] { ',' } , StringSplitOptions.RemoveEmptyEntries);
            var baseDn = ncs.Where(nc => nc.ToLower().StartsWith("dc="));
            return new LdapIdentity(string.Join(",", baseDn), IdentityType.DistinguishedName);
        }

        private static LdapIdentity Parse(string name, bool isUser)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            var identity = name.ToLower();

            //remove DOMAIN\\ prefix
            var index = identity.IndexOf("\\");
            if (index > 0)
            {
                identity = identity.Substring(index + 1);
            }

            var type = isUser ? IdentityType.Uid : IdentityType.Cn;

            if (identity.Contains("="))
            {
                type = IdentityType.DistinguishedName;
            }
            else if (identity.Contains("@"))
            {
                type = IdentityType.UserPrincipalName;
            }

            return new LdapIdentity(identity, type);
        }

        /// <summary>
        /// Converts DC=domain,DC=local to domain.local
        /// </summary>
        public static string DnToFqdn(string dn)
        {
            var ncs = dn.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            var fqdn = ncs.Select(nc => nc.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries)[1].TrimEnd(','));
            return string.Join(".", fqdn);
        }

        /// <summary>
        /// Extracts CN from DN
        /// </summary>
        public static string DnToCn(string dn)
        {
            if (string.IsNullOrWhiteSpace(dn)) throw new ArgumentException($"'{nameof(dn)}' cannot be null or whitespace.", nameof(dn));
            
            var splitted = dn.Split(',');
            if (splitted.Length == 0) throw new ArgumentException("Incorrect DistinguishedName format");

            var parts = splitted[0].Split("=");
            if (parts.Length < 2) throw new ArgumentException("Incorrect DistinguishedName format");

            return parts[1];
        }

        public string DnToFqdn()
        {
            return DnToFqdn(Name);
        }

        public bool IsChildOf(LdapIdentity parent)
        {
            return Name.EndsWith(parent.Name);
        }

        public override string ToString()
        {
            return Name;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;

            var other = obj as LdapIdentity;
            if (other == null) return false;
            if (other == this) return true;

            return other.Name == Name && other.Type == Type;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() + 23 + Type.GetHashCode();
        }
    }
}
