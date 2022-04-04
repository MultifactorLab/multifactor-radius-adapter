using MultiFactor.Radius.Adapter.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace MultiFactor.Radius.Adapter.Configuration
{
    public class ClientConfiguration
    {
        public ClientConfiguration()
        {
            BypassSecondFactorWhenApiUnreachable = true; //by default
        }

        /// <summary>
        /// Friendly client name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Client IP address
        /// </summary>
        public IPAddress? Ip { get; set; }

        /// <summary>
        /// Shared secret between this service and Radius client
        /// </summary>
        public string RadiusSharedSecret { get; set; }

        /// <summary>
        /// Where to handle first factor (UserName and Password)
        /// </summary>
        public AuthenticationSource FirstFactorAuthenticationSource { get; set; }

        /// <summary>
        /// Bypass second factor when MultiFactor API is unreachable
        /// </summary>
        public bool BypassSecondFactorWhenApiUnreachable { get; set; }

        #region ActiveDirectory Authentication settings

        /// <summary>
        /// Active Directory Domain
        /// </summary>
        public string ActiveDirectoryDomain { get; set; }

        /// <summary>
        /// LDAP bind distinguished name
        /// </summary>
        public string LdapBindDn { get; set; }

        /// <summary>
        /// Only members of this group allowed to access (Optional)
        /// </summary>
        public string ActiveDirectoryGroup { get; set; }

        /// <summary>
        /// Only members of this group required to pass 2fa to access (Optional)
        /// </summary>
        public string ActiveDirectory2FaGroup { get; set; }

        /// <summary>
        /// Use ActiveDirectory User general properties phone number (Optional)
        /// </summary>
        public bool UseActiveDirectoryUserPhone { get; set; }

        /// <summary>
        /// Use ActiveDirectory User general properties mobile phone number (Optional)
        /// </summary>
        public bool UseActiveDirectoryMobileUserPhone { get; set; }

        #endregion

        #region RADIUS Authentication settings

        /// <summary>
        /// This service RADIUS UDP Client endpoint
        /// </summary>
        public IPEndPoint ServiceClientEndpoint { get; set; }
        /// <summary>
        /// Network Policy Service RADIUS UDP Server endpoint
        /// </summary>
        public IPEndPoint NpsServerEndpoint { get; set; }

        #endregion

        /// <summary>
        /// Multifactor API KEY
        /// </summary>
        public string NasIdentifier { get; set; }
        /// <summary>
        /// RADIUS Shared Secret
        /// </summary>
        public string MultiFactorSharedSecret { get; set; }


        /// <summary>
        /// Custom RADIUS reply attributes
        /// </summary>
        public IDictionary<string, List<RadiusReplyAttributeValue>> RadiusReplyAttributes { get; set; }

        public IList<string> GetLdapReplyAttributes()
        {
            return RadiusReplyAttributes
                .Values
                .SelectMany(attr => attr)
                .Where(attr => attr.FromLdap)
                .Select(attr => attr.LdapAttributeName)
                .ToList();
        }

        public bool ShouldLoadUserGroups()
        {
            return 
                ActiveDirectoryGroup != null ||
                ActiveDirectory2FaGroup != null || 
                RadiusReplyAttributes
                    .Values
                    .SelectMany(attr => attr)
                    .Any(attr => attr.IsMemberOf || attr.UserGroupCondition != null);
        }
    }
}
