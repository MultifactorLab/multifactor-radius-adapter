using System.DirectoryServices.Protocols;
using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Entry;
using Multifactor.Core.Ldap.Extensions;
using Multifactor.Core.Ldap.LangFeatures;
using Multifactor.Core.Ldap.Name;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.Ldap;
using Multifactor.Radius.Adapter.v2.Core.Ldap.Forest;

namespace Multifactor.Radius.Adapter.v2.Services.LdapForest
{
    public class ForestSchemaLoader : IForestSchemaLoader
    {
        private readonly IDomainPermissionRules? _permissionRules;
        private readonly ILogger _logger;
        private readonly ILdapConnection _connection;

        private const string CommonNameAttribute = "cn";
        private const string UpnSuffixesAttribute = "uPNSuffixes";

        public ForestSchemaLoader(ILdapConnection connection, ILogger logger, IDomainPermissionRules? permissionRules = null)
        {
            Throw.IfNull(connection, nameof(connection));
            Throw.IfNull(logger, nameof(logger));
            
            _permissionRules = permissionRules;
            _logger = logger;
            _connection = connection;
        }

        public IForestSchema Load(DistinguishedName root)
        {
            if (root is null)
                throw new ArgumentNullException(nameof(root));
            _logger.LogDebug("Loading forest schema from {Root:l}", root);

            var domainNameSuffixes = new Dictionary<string, DistinguishedName>();
            try
            {
                var schema = new List<DistinguishedName> { root };
                var trustedDomains = GetTrustedDomains(root);
                foreach (var domainDn in trustedDomains)
                {
                    _logger.LogDebug("Found trusted domain: {Domain:l}", domainDn);
                    schema.Add(domainDn);
                }

                foreach (var domainDn in schema)
                {
                    FillDomainSuffixes(domainDn, schema, domainNameSuffixes);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to load forest schema");
            }

            return new ForestSchema(root, domainNameSuffixes);
        }

        private IEnumerable<DistinguishedName> GetTrustedDomains(DistinguishedName root)
        {
            var trustedDomainsResult = _connection.Find(
                new DistinguishedName("CN=System," + root.StringRepresentation),
                "objectClass=trustedDomain",
                SearchScope.OneLevel,
                attributes: CommonNameAttribute);

            var trustedDomains = trustedDomainsResult
                .Select(x => GetAttributeValue(x, CommonNameAttribute))
                .Where(x => x.Count != 0)
                .SelectMany(x => x)
                .Where(domain => _permissionRules?.IsPermittedDomain(domain) ?? true)
                .Select(LdapNamesUtils.FqdnToDn);

            return trustedDomains;
        }

        private void FillDomainSuffixes(
            DistinguishedName domainDn,
            List<DistinguishedName> schema,
            Dictionary<string, DistinguishedName> domainNameSuffixes)
        {
            var domainSuffix = LdapNamesUtils.DnToFqdn(domainDn);
            domainNameSuffixes.TryAdd(domainSuffix, domainDn);

            var isChild = schema.Any(parent => IsChildOf(domainDn, parent));
            if (isChild)
                return;
            try
            {
                var upnSuffixes = GetUpnSuffixes(domainDn);

                foreach (var suffix in upnSuffixes.Where(upn => !domainNameSuffixes.ContainsKey(upn)))
                {
                    domainNameSuffixes.Add(suffix, domainDn);
                    _logger.LogDebug("Found alternative UPN suffix {Suffix:l} for domain {Domain}", suffix, domainDn);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to query {Domain:l}", domainDn);
            }
        }

        private List<string> GetUpnSuffixes(DistinguishedName domainDn)
        {
            var upnSuffixesResult = _connection.Find(
                new DistinguishedName($"CN=Partitions,CN=Configuration,{domainDn.StringRepresentation}"),
                "objectClass=*",
                SearchScope.Base,
                attributes: UpnSuffixesAttribute);

            List<string> upnSuffixes = upnSuffixesResult
                .Select(x => GetAttributeValue(x, UpnSuffixesAttribute))
                .Where(x => x.Count != 0)
                .SelectMany(x => x)
                .ToList();

            return upnSuffixes;
        }

        private IReadOnlyCollection<string> GetAttributeValue(LdapEntry entry, string attributeName)
        {
            var attribute = entry.Attributes[attributeName];
            if (attribute is null)
                return Array.Empty<string>();

            return attribute.GetNotEmptyValues();
        }

        private bool IsChildOf(DistinguishedName child, DistinguishedName parent)
        {
            return child != parent && child.StringRepresentation.EndsWith(parent.StringRepresentation);
        }
    }
}