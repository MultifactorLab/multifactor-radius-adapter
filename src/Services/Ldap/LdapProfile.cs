using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MultiFactor.Radius.Adapter.Services.Ldap
{
    public class LdapProfile
    {
        public LdapProfile()
        {
            LdapAttrs = new Dictionary<string, object>();
        }

        public string DistinguishedName { get; set; }

        public string DistinguishedNameEscaped
        {
            get
            {
                var ret = DistinguishedName
                    .Replace("(", @"\28")
                    .Replace(")", @"\29");
                    
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    ret = ret.Replace("\"", "\\\""); //quotes
                    ret = ret.Replace("\\,", "\\5C,"); //comma
                }

                return ret;
            }
        }

        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Mobile { get; set; }
        public IList<string> MemberOf { get; set; }

        public LdapIdentity BaseDn { get; set; }

        public IDictionary<string, object> LdapAttrs { get; set; }
    }
}
