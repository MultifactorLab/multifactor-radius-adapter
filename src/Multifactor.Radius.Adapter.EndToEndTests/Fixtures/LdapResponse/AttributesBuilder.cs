using LdapForNet;

namespace Multifactor.Radius.Adapter.EndToEndTests.Fixtures.LdapResponse
{
    internal class AttributesBuilder(SearchResultAttributeCollection attributes)
    {
        private readonly SearchResultAttributeCollection _attributes = attributes ?? throw new ArgumentNullException(nameof(attributes));

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
