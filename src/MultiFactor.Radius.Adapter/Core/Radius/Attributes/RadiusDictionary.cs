//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

//MIT License

//Copyright(c) 2017 Verner Fortelius

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MultiFactor.Radius.Adapter.Core.Radius.Attributes
{
    public class RadiusDictionary : IRadiusDictionary
    {
        private readonly Dictionary<byte, DictionaryAttribute> _attributes = new();
        private readonly List<DictionaryVendorAttribute> _vendorSpecificAttributes = new();
        private readonly Dictionary<string, DictionaryAttribute> _attributeNames = new();
        private readonly ApplicationVariables _variables;

        /// <summary>
        /// Load the dictionary from a dictionary file
        /// </summary>        
        public RadiusDictionary(ApplicationVariables variables)
        {
            _variables = variables;
        }

        public void Read()
        {
            var path = $"{_variables.AppPath}{Path.DirectorySeparatorChar}content{Path.DirectorySeparatorChar}radius.dictionary";
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

        public DictionaryVendorAttribute GetVendorAttribute(uint vendorId, byte vendorCode)
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
