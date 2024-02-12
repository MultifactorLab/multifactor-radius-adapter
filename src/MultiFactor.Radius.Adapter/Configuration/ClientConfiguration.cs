using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Configuration.Features.AuthenticatedClientCacheFeature;
using MultiFactor.Radius.Adapter.Configuration.Features.PrivacyModeFeature;
using MultiFactor.Radius.Adapter.Configuration.Features.UserNameTransformFeature;
using MultiFactor.Radius.Adapter.Core;
using MultiFactor.Radius.Adapter.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace MultiFactor.Radius.Adapter.Configuration
{
    public class ClientConfiguration : IClientConfiguration, IClientConfigurationBuilder
    {
        private ClientConfiguration(string name, string rdsSharedSecret, AuthenticationSource firstFactorAuthSource,
            string apiKey, string apiSecret)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException($"'{nameof(name)}' cannot be null or whitespace.", nameof(name));      

            if (string.IsNullOrWhiteSpace(rdsSharedSecret))
                throw new ArgumentException($"'{nameof(rdsSharedSecret)}' cannot be null or whitespace.", nameof(rdsSharedSecret));         

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException($"'{nameof(apiKey)}' cannot be null or whitespace.", nameof(apiKey));

            if (string.IsNullOrWhiteSpace(apiSecret))
                throw new ArgumentException($"'{nameof(apiSecret)}' cannot be null or whitespace.", nameof(apiSecret));

            BypassSecondFactorWhenApiUnreachable = true; //by default
            LoadActiveDirectoryNestedGroups = true;

            Name = name;
            RadiusSharedSecret = rdsSharedSecret;
            FirstFactorAuthenticationSource = firstFactorAuthSource;
            ApiCredential = new MultifactorApiCredential(apiKey, apiSecret);
        }

        public static IClientConfigurationBuilder CreateBuilder(string name, string rdsSharedSecret, AuthenticationSource firstFactorAuthSource, 
            string apiKey, string apiSecret)
        {
            return new ClientConfiguration(name, rdsSharedSecret, firstFactorAuthSource, apiKey, apiSecret);
        }

        /// <summary>
        /// Friendly client name
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Shared secret between this service and Radius client
        /// </summary>
        public string RadiusSharedSecret { get; }

        /// <summary>
        /// Where to handle first factor (UserName and Password)
        /// </summary>
        public AuthenticationSource FirstFactorAuthenticationSource { get; }

        public MultifactorApiCredential ApiCredential { get; }

        /// <summary>
        /// Load user profile from AD and check group membership and 
        /// </summary>
        public bool CheckMembership
        {
            get
            {
                return ActiveDirectoryDomain != null &&
                    (ActiveDirectoryGroups.Any() ||
                    ActiveDirectory2FaGroup.Any() ||
                    ActiveDirectory2FaBypassGroup.Any() ||
                    PhoneAttributes.Any() ||
                    RadiusReplyAttributes
                        .Values
                        .SelectMany(attr => attr)
                        .Any(attr => attr.FromLdap || attr.IsMemberOf || attr.UserGroupCondition != null));
            }
        }

        public string[] SplittedActiveDirectoryDomains =>
            (ActiveDirectoryDomain ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Distinct()
            .ToArray();

        /// <summary>
        /// Bypass second factor when MultiFactor API is unreachable
        /// </summary>
        public bool BypassSecondFactorWhenApiUnreachable { get; private set; }

        public PrivacyModeDescriptor PrivacyMode { get; private set; }

        /// <summary>
        /// Active Directory Domain
        /// </summary>
        public string ActiveDirectoryDomain { get; private set; }

        /// <summary>
        /// LDAP bind distinguished name
        /// </summary>
        public string LdapBindDn { get; private set; }


        private readonly List<string> _activeDirectoryGroups = new ();
        /// <summary>
        /// Only members of this group allowed to access (Optional)
        /// </summary>
        public string[] ActiveDirectoryGroups => _activeDirectoryGroups.ToArray();


        private readonly List<string> _activeDirectory2FaGroup = new();
        /// <summary>
        /// Only members of this group required to pass 2fa to access (Optional)
        /// </summary>
        public string[] ActiveDirectory2FaGroup => _activeDirectory2FaGroup.ToArray();


        private readonly List<string> _activeDirectory2FaBypassGroup = new();
        /// <summary>
        /// Members of this group should not pass 2fa to access (Optional)
        /// </summary>
        public string[] ActiveDirectory2FaBypassGroup => _activeDirectory2FaBypassGroup.ToArray();


        private readonly List<string> _phoneAttrs = new();
        /// <summary>
        /// AD attribute name(s) where to search phone number
        /// </summary>
        public string[] PhoneAttributes => _phoneAttrs.ToArray(); 

        public bool LoadActiveDirectoryNestedGroups { get; private set; }

        //Lookup for UPN and use it instead of uid
        public bool UseUpnAsIdentity { get; private set; }

        /// <summary>
        /// This service RADIUS UDP Client endpoint
        /// </summary>
        public IPEndPoint ServiceClientEndpoint { get; private set; }
        /// <summary>
        /// Network Policy Service RADIUS UDP Server endpoint
        /// </summary>
        public IPEndPoint NpsServerEndpoint { get; private set; }
        public LdapCatalogType LdapCatalogType { get; private set; }
        public string ServiceAccountUser { get; private set; }

        public string ServiceAccountPassword { get; private set; }

        /// <summary>
        /// Groups to assign to the registered user.Specified groups will be assigned to a new user.
        /// Syntax: group names (from your Management Portal) separated by semicolons.
        /// <para>
        /// Example: group1;Group Name Two;
        /// </para>
        /// </summary>
        public string SignUpGroups { get; private set; }

        public AuthenticatedClientCacheConfig AuthenticationCacheLifetime { get; private set; }

        private readonly Dictionary<string, RadiusReplyAttributeValue[]> _radiusReplyAttributes = new();

        /// <summary>
        /// Custom RADIUS reply attributes
        /// </summary>
        public IReadOnlyDictionary<string, RadiusReplyAttributeValue[]> RadiusReplyAttributes => _radiusReplyAttributes;

        private readonly List<UserNameTransformRulesElement> _userNameTransformRules = new();
        /// <summary>
        /// Username transfor rules
        /// </summary>
        public UserNameTransformRulesElement[] UserNameTransformRules => _userNameTransformRules.ToArray();

        public string CallingStationIdVendorAttribute { get; private set; }

        public bool ShouldLoadUserGroups()
        {
            return
                ActiveDirectoryGroups.Any() ||
                ActiveDirectory2FaGroup.Any() ||
                ActiveDirectory2FaBypassGroup.Any() ||
                RadiusReplyAttributes
                    .Values
                    .SelectMany(attr => attr)
                    .Any(attr => attr.IsMemberOf || attr.UserGroupCondition != null);
        }

        public IClientConfigurationBuilder SetBypassSecondFactorWhenApiUnreachable(bool val)
        {
            BypassSecondFactorWhenApiUnreachable = val;
            return this;
        }

        public IClientConfigurationBuilder SetPrivacyMode(PrivacyModeDescriptor val)
        {
            PrivacyMode = val;
            return this;
        }

        public IClientConfigurationBuilder SetActiveDirectoryDomain(string val)
        {
            if (string.IsNullOrWhiteSpace(val))
            {
                throw new ArgumentException($"'{nameof(val)}' cannot be null or whitespace.", nameof(val));
            }

            ActiveDirectoryDomain = val;
            return this;
        }

        public IClientConfigurationBuilder SetLdapBindDn(string val)
        {
            if (string.IsNullOrWhiteSpace(val))
            {
                throw new ArgumentException($"'{nameof(val)}' cannot be null or whitespace.", nameof(val));
            }

            LdapBindDn = val;
            return this;
        }

        public IClientConfigurationBuilder AddActiveDirectoryGroup(string val)
        {
            if (string.IsNullOrWhiteSpace(val))
            {
                throw new ArgumentException($"'{nameof(val)}' cannot be null or whitespace.", nameof(val));
            }

            _activeDirectoryGroups.Add(val);
            return this;
        }
        
        public IClientConfigurationBuilder AddActiveDirectoryGroups(IEnumerable<string> values)
        {
            if (values is null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            _activeDirectoryGroups.AddRange(values);
            return this;
        }

        public IClientConfigurationBuilder AddActiveDirectory2FaGroup(string val)
        {
            if (string.IsNullOrWhiteSpace(val))
            {
                throw new ArgumentException($"'{nameof(val)}' cannot be null or whitespace.", nameof(val));
            }

            _activeDirectory2FaGroup.Add(val);
            return this;
        }

        public IClientConfigurationBuilder AddActiveDirectory2FaGroups(IEnumerable<string> values)
        {
            if (values is null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            _activeDirectory2FaGroup.AddRange(values); 
            return this;
        }

        public IClientConfigurationBuilder AddActiveDirectory2FaBypassGroup(string val)
        {
            if (string.IsNullOrWhiteSpace(val))
            {
                throw new ArgumentException($"'{nameof(val)}' cannot be null or whitespace.", nameof(val));
            }

            _activeDirectory2FaBypassGroup.Add(val);
            return this;
        }
        
        public IClientConfigurationBuilder AddActiveDirectory2FaBypassGroups(IEnumerable<string> values)
        {
            if (values is null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            _activeDirectory2FaBypassGroup.AddRange(values);
            return this;
        }

        public IClientConfigurationBuilder AddPhoneAttribute(string phoneAttr)
        {
            if (string.IsNullOrWhiteSpace(phoneAttr))
            {
                throw new ArgumentException($"'{nameof(phoneAttr)}' cannot be null or whitespace.", nameof(phoneAttr));
            }

            _phoneAttrs.Add(phoneAttr);
            return this;
        }
        
        public IClientConfigurationBuilder AddPhoneAttributes(IEnumerable<string> attributes)
        {
            if (attributes is null)
            {
                throw new ArgumentNullException(nameof(attributes));
            }

            _phoneAttrs.AddRange(attributes);
            return this;
        }

        public IClientConfigurationBuilder SetLoadActiveDirectoryNestedGroups(bool val)
        {
            LoadActiveDirectoryNestedGroups = val;
            return this;
        }

        public IClientConfigurationBuilder SetUseUpnAsIdentity(bool val)
        {
            UseUpnAsIdentity = val;
            return this;
        }

        public IClientConfigurationBuilder SetServiceClientEndpoint(IPEndPoint val)
        {
            ServiceClientEndpoint = val;
            return this;
        }

        public IClientConfigurationBuilder SetNpsServerEndpoint(IPEndPoint val)
        {
            NpsServerEndpoint = val;
            return this;
        }

        public IClientConfigurationBuilder SetLdapCatalogType(LdapCatalogType catalogType) 
        {
            LdapCatalogType = catalogType;
            return this;
        }

        public IClientConfigurationBuilder SetServiceAccountUser(string val)
        {
            ServiceAccountUser = val;
            return this;
        }

        public IClientConfigurationBuilder SetServiceAccountPassword(string val)
        {
            ServiceAccountPassword = val;
            return this;
        }

        public IClientConfigurationBuilder SetSignUpGroups(string val)
        {
            SignUpGroups = val;
            return this;
        }

        public IClientConfigurationBuilder SetAuthenticationCacheLifetime(AuthenticatedClientCacheConfig val)
        {
            AuthenticationCacheLifetime = val;
            return this;
        }

        public IClientConfigurationBuilder AddRadiusReplyAttribute(string attr, IEnumerable<RadiusReplyAttributeValue> values)
        {
            if (string.IsNullOrWhiteSpace(attr))
            {
                throw new ArgumentException($"'{nameof(attr)}' cannot be null or whitespace.", nameof(attr));
            }

            if (values is null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            _radiusReplyAttributes[attr] = values.ToArray();
            return this;
        }

        public IClientConfigurationBuilder AddUserNameTransformRule(UserNameTransformRulesElement rule)
        {
            if (rule is null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            _userNameTransformRules.Add(rule);
            return this;
        }

        public IClientConfigurationBuilder SetCallingStationIdVendorAttribute(string val)
        {
            if (string.IsNullOrWhiteSpace(val))
            {
                throw new ArgumentException($"'{nameof(val)}' cannot be null or whitespace.", nameof(val));
            }

            CallingStationIdVendorAttribute = val;
            return this;
        }

        public IClientConfiguration Build() => this;
    }
}
