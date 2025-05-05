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

namespace Multifactor.Radius.Adapter.v2.Core.Radius.Attributes
{
    public class DictionaryAttribute
    {
        public const string TypeString = "string";
        public const string TypeTaggedString = "tagged-string";
        public const string TypeInteger = "integer";
        public const string TypeTaggedInteger = "tagged-integer";
        public const string TypeOctet = "octet";
        public const string TypeIpAddr = "ipaddr";

        public readonly byte Code;
        public readonly string Name;
        public readonly string Type;

        /// <summary>
        /// Create a dictionary rfc attribute
        /// </summary>
        public DictionaryAttribute(string name, byte code, string type)
        {
            Code = code;
            Name = name;
            Type = type;
        }
    }
}
