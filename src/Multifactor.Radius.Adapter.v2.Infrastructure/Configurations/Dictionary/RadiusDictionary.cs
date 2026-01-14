using System.Text;
using DictionaryAttribute = Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Dictionary.Attributes.DictionaryAttribute;
using DictionaryVendorAttribute = Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Dictionary.Attributes.DictionaryVendorAttribute;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Dictionary
{
    public class RadiusDictionary : IRadiusDictionary
    {
        private readonly Dictionary<byte, DictionaryAttribute> _attributes = new();
        private readonly Dictionary<(uint VendorId, byte VendorCode), DictionaryVendorAttribute> _vendorAttributes = new();
        private readonly Dictionary<string, DictionaryAttribute> _attributeNames = new();
        private readonly string _filePath;

        public RadiusDictionary(string? filePath = null, string? appPath = null)
        {
            _filePath = ResolveFilePath(filePath, appPath);
        }

        private string ResolveFilePath(string? filePath, string? appPath)
        {
            if (!string.IsNullOrEmpty(filePath) && Path.IsPathRooted(filePath))
                return filePath;

            var basePath = string.IsNullOrEmpty(appPath) 
                ? AppDomain.CurrentDomain.BaseDirectory 
                : appPath;

            var relativePath = string.IsNullOrEmpty(filePath)
                ? Path.Combine("content", "radius.dictionary")
                : filePath;

            return Path.Combine(basePath, relativePath);
        }

        public void Read()
        {
            if (!File.Exists(_filePath))
                throw new FileNotFoundException($"Dictionary file not found: {_filePath}");

            using var reader = new StreamReader(_filePath, Encoding.UTF8);
            
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                ProcessLine(line.Trim());
            }

            GetInfo();
        }

        private void ProcessLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                return;

            var parts = SplitLine(line);
            
            if (parts.Length < 2) return;

            switch (parts[0].ToUpperInvariant())
            {
                case "ATTRIBUTE":
                    ParseAttribute(parts);
                    break;
                case "VENDORSPECIFICATTRIBUTE":
                    ParseVendorAttribute(parts);
                    break;
            }
        }

        private string[] SplitLine(string line)
        {
            return line.Split(['\t', ' ', '\''], StringSplitOptions.RemoveEmptyEntries);
        }

        private void ParseAttribute(string[] parts)
        {
            if (parts.Length < 4) return;

            if (!byte.TryParse(parts[1], out byte typeCode))
                return;

            var name = parts[2];
            var dataType = parts[3];

            var attribute = new DictionaryAttribute(name, typeCode, dataType);
            
            // Обновляем существующие записи
            _attributes[typeCode] = attribute;
            _attributeNames[name] = attribute;
        }

        private void ParseVendorAttribute(string[] parts)
        {
            if (parts.Length < 5) return;

            if (!uint.TryParse(parts[1], out uint vendorId) ||
                !byte.TryParse(parts[2], out byte vendorCode))
                return;

            var name = parts[3];
            var dataType = parts[4];

            var vsa = new DictionaryVendorAttribute(vendorId, name, vendorCode, dataType);
            
            var key = (vendorId, vendorCode);
            
            // Обновляем существующие записи
            _vendorAttributes[key] = vsa;
            _attributeNames[name] = vsa;
        }

        public string GetInfo()
        {
            return $"Parsed {_attributes.Count} attributes and {_vendorAttributes.Count} vendor attributes";
        }

        public DictionaryVendorAttribute? GetVendorAttribute(uint vendorId, byte vendorCode)
        {
            var key = (vendorId, vendorCode);
            return _vendorAttributes.TryGetValue(key, out var attribute) ? attribute : null;
        }

        public DictionaryAttribute GetAttribute(byte code)
        {
            if (_attributes.TryGetValue(code, out var attribute))
                return attribute;
                
            throw new KeyNotFoundException($"Attribute with code {code} not found");
        }

        public DictionaryAttribute GetAttribute(string name)
        {
            if (_attributeNames.TryGetValue(name, out var attribute))
                return attribute;
                
            throw new KeyNotFoundException($"Attribute with name '{name}' not found");
        }
    }
}