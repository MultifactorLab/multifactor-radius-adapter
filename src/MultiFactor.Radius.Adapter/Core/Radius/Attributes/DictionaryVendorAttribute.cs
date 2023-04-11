﻿//Copyright(c) 2020 MultiFactor
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

namespace MultiFactor.Radius.Adapter.Core.Radius.Attributes
{
    public class DictionaryVendorAttribute : DictionaryAttribute
    {
        public readonly uint VendorId;
        public readonly uint VendorCode;


        /// <summary>
        /// Create a dictionary vendor specific attribute
        /// </summary>
        /// <param name="vendorId"></param>
        /// <param name="name"></param>
        /// <param name="vendorCode"></param>        
        /// <param name="type"></param>
        public DictionaryVendorAttribute(uint vendorId, string name, uint vendorCode, string type) : base(name, 26, type)
        {
            VendorId = vendorId;
            VendorCode = vendorCode;
        }
    }
}