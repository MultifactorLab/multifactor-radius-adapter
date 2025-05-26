using Multifactor.Core.Ldap.LangFeatures;
using Multifactor.Core.Ldap.Name;
using Multifactor.Radius.Adapter.v2.Core.Ldap.Forest;

namespace Multifactor.Radius.Adapter.v2.Services.LdapForest
{
    /// <summary>
    /// Thread-Safe forest metadata cache.
    /// </summary>
    public class ForestMetadataCache : IForestMetadataCache
    {
        private readonly object _locker = new object();
        private readonly Dictionary<string, ForestMetadata> _cache = new();

        /// <summary>
        /// Returns information about specified domain forest.
        /// </summary>
        /// <param name="key">Client configuration friendly name.</param>
        /// <param name="rootDomain">Root domain.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public IForestSchema? Get(string key, DistinguishedName rootDomain)
        {
            if (key is null) throw new ArgumentNullException(nameof(key));
            if (rootDomain is null) throw new ArgumentNullException(nameof(rootDomain));

            lock (_locker)
            {
                if (_cache.TryGetValue(key, out var value))
                {
                    return value[rootDomain];
                }

                return null;
            }
        }

        public void Add(string key, IForestSchema forestSchema)
        {
            Throw.IfNull(forestSchema, nameof(forestSchema));
                
            lock (_locker)
            {
                if (_cache.TryGetValue(key, out var value))
                {
                    value.Add(forestSchema.Root, forestSchema);
                }
                else
                {
                    var meta = new ForestMetadata();
                    meta.Add(forestSchema.Root, forestSchema);
                    _cache[key] = meta;
                }
            }
        }

        /// <summary>
        /// Returns true if the forest info already exists for the specified client and domain.
        /// </summary>
        /// <param name="key">Client configuration friendly name.</param>
        /// <param name="rootDomain">Root domain.</param>
        /// <returns></returns>
        public bool HasSchema(string key, DistinguishedName rootDomain)
        {
            lock (_locker)
            {
                return _cache.ContainsKey(key) && _cache[key].HasSchema(rootDomain);
            }
        }
    }
}
