using MultiFactor.Radius.Adapter.Core.Services.Ldap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Services.Ldap
{
    /// <summary>
    /// LDAP catalog objects and attributes names
    /// </summary>
    public class LdapNames
    {
        private LdapServerType _serverType;

        public string Uid
        {
            get
            {
                switch (_serverType)
                {
                    case LdapServerType.Generic:
                        return "uid";
                    case LdapServerType.ActiveDirectory:
                        return "sAMAccountName";
                    default:
                        throw new NotImplementedException(_serverType.ToString());
                }
            }
        }

        public string Cn
        {
            get
            {
                switch (_serverType)
                {
                    case LdapServerType.Generic:
                        return "cn";
                    case LdapServerType.ActiveDirectory:
                        return "name";
                    default:
                        throw new NotImplementedException(_serverType.ToString());
                }
            }
        }

        public string UserClass
        {
            get
            {
                switch (_serverType)
                {
                    case LdapServerType.Generic:
                        return "person";
                    case LdapServerType.ActiveDirectory:
                        return "user";
                    default:
                        throw new NotImplementedException(_serverType.ToString());
                }
            }
        }

        public string GroupClass
        {
            get
            {
                switch (_serverType)
                {
                    case LdapServerType.Generic:
                        return "nestedgroup";
                    case LdapServerType.ActiveDirectory:
                        return "group";
                    default:
                        throw new NotImplementedException(_serverType.ToString());
                }
            }
        }

        public string ObjectClass 
        {
            get
            {
                switch (_serverType)
                {
                    case LdapServerType.Generic:
                        return "objectClass";
                    case LdapServerType.ActiveDirectory:
                        return "objectCategory";
                    default:
                        throw new NotImplementedException(_serverType.ToString());
                }
            }
        }
        
        public LdapNames(LdapServerType serverType)
        {
            _serverType = serverType;
        }

        public string Identity(LdapIdentity identity)
        {
            switch (identity.Type)
            {
                case IdentityType.DistinguishedName:
                    return "distinguishedName";
                case IdentityType.Uid:
                    return Uid;
                case IdentityType.UserPrincipalName:
                    return "userPrincipalName";
                case IdentityType.Cn:
                    return Cn;
                default:
                    throw new NotImplementedException(identity.Type.ToString());
            }
        }
    }
}
