using Multifactor.Core.Ldap.LangFeatures;
using Multifactor.Core.Ldap.Name;

namespace Multifactor.Radius.Adapter.v2.Core.Ldap.Forest
{
    /// <summary>
    /// Information about domain controller forest.
    /// </summary>
    public class ForestSchema : IForestSchema
    {
        private readonly IReadOnlyDictionary<string, DistinguishedName> _domainNameSuffixes;
        public DistinguishedName Root { get; }

        public IReadOnlyDictionary<string, DistinguishedName> DomainNameSuffixes => _domainNameSuffixes;

        public ForestSchema(DistinguishedName root, IReadOnlyDictionary<string, DistinguishedName> domainNameSuffixes)
        {
            Throw.IfNull(domainNameSuffixes, nameof(domainNameSuffixes));
            Throw.IfNull(root, nameof(root));
            
            _domainNameSuffixes = domainNameSuffixes;
            Root = root;
        }

        public DistinguishedName? GetMostRelevantDomainBySuffix(string suffix)
        {
            Throw.IfNullOrWhiteSpace(suffix, nameof(suffix));
            var targetSuffix = suffix.ToLower();
            //best match
            foreach (var key in _domainNameSuffixes.Keys)
            {
                if (targetSuffix.Equals(key, StringComparison.CurrentCultureIgnoreCase))
                {
                    return _domainNameSuffixes[key];
                }
            }

            //approximately match
            foreach (var key in _domainNameSuffixes.Keys)
            {
                if (targetSuffix.EndsWith(key.ToLower()))
                {
                    return _domainNameSuffixes[key];
                }
            }

            //netibosname match
            foreach (var key in _domainNameSuffixes.Keys)
            {
                if (key.StartsWith(targetSuffix, StringComparison.CurrentCultureIgnoreCase))
                {
                    return _domainNameSuffixes[key];
                }
            }

            return null;
        }

        public DistinguishedName FindDomainByNetbiosName(string netbiosName)
        {
            var matchedDomains = new List<DistinguishedName>();
            foreach (var suffix in _domainNameSuffixes.Keys)
            {
                if (suffix.StartsWith(netbiosName))
                    matchedDomains.Add(_domainNameSuffixes[suffix]);
            }

            var suitableDomains = matchedDomains.Distinct().ToList();

            if (suitableDomains.Count == 1)
                return suitableDomains.Single();
            if (suitableDomains.Count == 0)
                throw new Exception($"No domain was found for '{netbiosName}' netbiosName");

            throw new Exception($"Ambiguous domain for '{netbiosName}' netbiosName");
        }
    }
}
