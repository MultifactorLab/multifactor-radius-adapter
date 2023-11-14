using LdapForNet;

namespace MultiFactor.Radius.Adapter.Tests.Fixtures.LdapResponse
{
    internal class AttributesBuilder
    {
        private readonly SearchResultAttributeCollection _attributes;

        public AttributesBuilder(SearchResultAttributeCollection attributes)
        {
            _attributes = attributes ?? throw new ArgumentNullException(nameof(attributes));
        }

        public AttributesBuilder Add(string attribute, params string[] values)
        {
            if (string.IsNullOrWhiteSpace(attribute))
            {
                throw new ArgumentException($"'{nameof(attribute)}' cannot be null or whitespace.", nameof(attribute));
            }

            if (values is null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            var attr = new DirectoryAttribute { Name = attribute };
            attr.AddValues(values);
            _attributes.Add(attr);
            return this;
        }
    }
}
