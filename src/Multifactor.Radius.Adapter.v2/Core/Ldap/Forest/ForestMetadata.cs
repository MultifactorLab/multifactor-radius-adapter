using Multifactor.Core.Ldap.LangFeatures;
using Multifactor.Core.Ldap.Name;

namespace Multifactor.Radius.Adapter.v2.Core.Ldap.Forest
{
    public class ForestMetadata
    {
        private readonly Dictionary<DistinguishedName, IForestSchema> _forests = new();

        /// <summary>
        /// Returns information about forest of specified root domain.
        /// </summary>
        /// <param name="rootDomain">Root domain.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public IForestSchema? this[DistinguishedName rootDomain]
        {
            get
            {
                if (rootDomain == null) throw new ArgumentNullException(nameof(rootDomain));
                Throw.IfNull(rootDomain, nameof(rootDomain));

                return _forests.GetValueOrDefault(rootDomain);
            }
        }

        /// <summary>
        /// Adds forest information of specified domain.
        /// </summary>
        /// <param name="rootDomain">Root domain.</param>
        public void Add(DistinguishedName rootDomain, IForestSchema schema)
        {
            Throw.IfNull(schema, nameof(schema));
            
            _forests.TryAdd(rootDomain, schema);
        }

        /// <summary>
        /// Returns true if information about specified domain forest already exists in the current metadata object.
        /// </summary>
        /// <param name="rootDomain">Root domain.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public bool HasSchema(DistinguishedName rootDomain)
        {
            if (rootDomain is null) throw new ArgumentNullException(nameof(rootDomain));
            return _forests.ContainsKey(rootDomain);
        }
    }
}
