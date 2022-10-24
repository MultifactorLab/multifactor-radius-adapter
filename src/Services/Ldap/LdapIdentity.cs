using MultiFactor.Radius.Adapter.Core.Services.Ldap;
using System;
using System.Linq;

namespace MultiFactor.Radius.Adapter.Services.Ldap
{
    public class LdapIdentity
    {
        public string Name { get; set; }
        public IdentityType Type { get; set; }

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

            return new LdapIdentity
            {
                Name = string.Join(",", dn),
                Type = IdentityType.DistinguishedName
            };
        }
        
        /// <summary>
        /// DC part from DN
        /// </summary>
        public static LdapIdentity BaseDn(string dn)
        {
            var ncs = dn.Split(new[] { ',' } , StringSplitOptions.RemoveEmptyEntries);
            var baseDn = ncs.Where(nc => nc.ToLower().StartsWith("dc="));
            return new LdapIdentity
            {
                Type = IdentityType.DistinguishedName,
                Name = string.Join(",", baseDn)
            };
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

            return new LdapIdentity
            {
                Name = identity,
                Type = type
            };
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
            return dn.Split(',')[0].Split("=")[1];
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
    }
}
