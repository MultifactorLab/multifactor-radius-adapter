using System.Text;

namespace Multifactor.Radius.Adapter.v2.Core.Radius.Attributes
{
    public class RadiusDictionary : IRadiusDictionary
    {
        private readonly Dictionary<byte, DictionaryAttribute> _attributes = new();
        private readonly List<DictionaryVendorAttribute> _vendorSpecificAttributes = new();
        private readonly Dictionary<string, DictionaryAttribute> _attributeNames = new();
        private readonly ApplicationVariables _variables;
        private readonly string? _filePath;

        /// <summary>
        /// Load the dictionary from a dictionary file
        /// </summary>        
        public RadiusDictionary(ApplicationVariables variables, string? filePath = null)
        {
            _variables = variables;
            _filePath = filePath;
        }

        public void Read()
        {
            var stringBuilder = new StringBuilder(_variables.AppPath);
            stringBuilder.Append(_filePath ?? $"{Path.DirectorySeparatorChar}content{Path.DirectorySeparatorChar}radius.dictionary");
            
            var path = stringBuilder.ToString();
            using var sr = new StreamReader(path);
            
            while (sr.Peek() != -1)
            {
                var line = sr.ReadLine();

                if (line.StartsWith("Attribute"))
                {
                    var lineparts = line.Split(new char[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    var key = Convert.ToByte(lineparts[1]);

                    // If duplicates are encountered, the last one will prevail                        
                    if (_attributes.ContainsKey(key))
                    {
                        _attributes.Remove(key);
                    }

                    if (_attributeNames.ContainsKey(lineparts[2]))
                    {
                        _attributeNames.Remove(lineparts[2]);
                    }

                    var attributeDefinition = new DictionaryAttribute(lineparts[2], key, lineparts[3]);
                    _attributes.Add(key, attributeDefinition);
                    _attributeNames.Add(attributeDefinition.Name, attributeDefinition);

                    continue;
                }

                if (line.StartsWith("VendorSpecificAttribute"))
                {
                    var lineparts = line.Split(new char[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    var vsa = new DictionaryVendorAttribute(
                        Convert.ToUInt32(lineparts[1]),
                        lineparts[3],
                        Convert.ToUInt32(lineparts[2]),
                        lineparts[4]);

                    _vendorSpecificAttributes.Add(vsa);

                    if (_attributeNames.ContainsKey(vsa.Name))
                    {
                        _attributeNames.Remove(vsa.Name);
                    }

                    _attributeNames.Add(vsa.Name, vsa);

                    continue;
                }
            }    
        }

        public string GetInfo()
        {
            return $"Parsed {_attributes.Count} attributes and {_vendorSpecificAttributes.Count} vendor attributes from the radius.dictionary file";
        }

        public DictionaryVendorAttribute? GetVendorAttribute(uint vendorId, byte vendorCode)
        {
            return _vendorSpecificAttributes.FirstOrDefault(o => o.VendorId == vendorId && o.VendorCode == vendorCode);
        }

        public DictionaryAttribute GetAttribute(byte typecode)
        {
            return _attributes[typecode];
        }

        public DictionaryAttribute GetAttribute(string name)
        {
            _attributeNames.TryGetValue(name, out var attributeType);
            return attributeType;
        }
    }
}
