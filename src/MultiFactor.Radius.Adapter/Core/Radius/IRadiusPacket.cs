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

using System.Collections.Generic;
using System.Net;

namespace MultiFactor.Radius.Adapter.Core.Radius
{
    public interface IRadiusPacket
    {
        RadiusPacketHeader Header { get; }
        SharedSecret SharedSecret { get; }
        RadiusAuthenticator Authenticator { get; }
        byte[] RequestAuthenticator { get; }
        bool IsEapMessageChallenge { get; }
        bool IsVendorAclRequest { get; }
        bool IsWinLogon { get; }
        bool IsOpenVpnStaticChallenge { get; }

        AuthenticationType AuthenticationType { get; }

        /// <summary>
        /// Returns the User-Name attribute value.
        /// </summary>
        string UserName { get; }
        string TryGetUserPassword();
        string TryGetChallenge();
        string RemoteHostName { get; }
        string CallingStationId { get; }
        string CalledStationId { get; }
        string NasIdentifier { get; }

        IRadiusPacket CreateResponsePacket(PacketCode responseCode);

        T GetAttribute<T>(string name);
        string GetString(string name);

        void AddAttribute(string name, string value);
        void AddAttribute(string name, uint value);
        void AddAttribute(string name, IPAddress value);
        void AddAttribute(string name, byte[] value);
        
        void AddAttributes(IDictionary<string, object> attributes);
        
        IRadiusPacket UpdateAttribute(string name, string value);
        void CopyTo(IRadiusPacket packet);
        IRadiusPacket Clone();

        IDictionary<string, List<object>> Attributes { get; set; }

        string CreateUniqueKey(IPEndPoint remoteEndpoint);

        /// <summary>
        /// Adds new value for the attribute that will be returned instead of original value. The original value will not be deleted.
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="transformedValue"></param>
        void AddTransformation(string attribute, string transformedValue);
    }
}
