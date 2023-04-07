using LdapForNet;

namespace MultiFactor.Radius.Adapter.Tests.Fixtures.LdapResponse
{
    internal static class LdapEntryFactory
    {
        public static LdapEntry Create(string dn, Action<AttributesBuilder>? setAttributes = null)
        {
            if (string.IsNullOrWhiteSpace(dn))
            {
                throw new ArgumentException($"'{nameof(dn)}' cannot be null or whitespace.", nameof(dn));
            }

            var entry = new LdapEntry
            {
                Dn = dn,
                DirectoryAttributes = new SearchResultAttributeCollection()
            };

            var builder = new AttributesBuilder(entry.DirectoryAttributes);
            setAttributes?.Invoke(builder);

            return entry;
        }
    }
}
