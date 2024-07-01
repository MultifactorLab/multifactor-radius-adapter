using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Core.Services.Ldap;
using System;

namespace MultiFactor.Radius.Adapter.Services.Ldap
{
    /// <summary>
    /// LDAP catalog objects and attributes names
    /// </summary>
    public class LdapNames
    {
        private readonly LdapServerType _serverType;

        public string Uid
        {
            get
            {
                return _serverType switch
                {
                    LdapServerType.Generic => "uid",
                    LdapServerType.ActiveDirectory => "sAMAccountName",
                    _ => throw new NotImplementedException(_serverType.ToString()),
                };
            }
        }

        public string Cn
        {
            get
            {
                return _serverType switch
                {
                    LdapServerType.Generic => "cn",
                    LdapServerType.ActiveDirectory => "name",
                    _ => throw new NotImplementedException(_serverType.ToString()),
                };
            }
        }

        public string UserClass
        {
            get
            {
                return _serverType switch
                {
                    LdapServerType.Generic => "person",
                    LdapServerType.ActiveDirectory => "user",
                    _ => throw new NotImplementedException(_serverType.ToString()),
                };
            }
        }

        public string GroupClass
        {
            get
            {
                return _serverType switch
                {
                    LdapServerType.Generic => "nestedgroup",
                    LdapServerType.ActiveDirectory => "group",
                    _ => throw new NotImplementedException(_serverType.ToString()),
                };
            }
        }

        public string ObjectClass 
        {
            get
            {
                return _serverType switch
                {
                    LdapServerType.Generic => "objectClass",
                    LdapServerType.ActiveDirectory => "objectCategory",
                    _ => throw new NotImplementedException(_serverType.ToString()),
                };
            }
        }
        
        public LdapNames(LdapServerType serverType)
        {
            _serverType = serverType;
        }

        public string Identity(LdapIdentity identity)
        {
            return identity.Type switch
            {
                IdentityType.DistinguishedName => "distinguishedName",
                IdentityType.Uid => Uid,
                IdentityType.UserPrincipalName => "userPrincipalName",
                IdentityType.Cn => Cn,
                _ => throw new NotImplementedException(identity.Type.ToString()),
            };
        }

        public static LdapNames Create(AuthenticationSource source, bool isFreeIpa)
        {
            if (isFreeIpa || source == AuthenticationSource.Ldap)
            {
                return new LdapNames(LdapServerType.Generic);
            }
            return source switch
            {
                AuthenticationSource.ActiveDirectory
                    or AuthenticationSource.Radius
                    or AuthenticationSource.None => new LdapNames(LdapServerType.ActiveDirectory),
                _ => throw new NotImplementedException(source.ToString()),
            };
        }
    }
}
