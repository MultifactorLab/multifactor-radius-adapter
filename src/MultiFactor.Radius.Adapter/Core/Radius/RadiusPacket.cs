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
using System.Linq;
using System.Net;
using System.Text;

namespace MultiFactor.Radius.Adapter.Core.Radius
{
    /// <summary>
    /// This class encapsulates a Radius packet and presents it in a more readable form
    /// </summary>
    public class RadiusPacket : IRadiusPacket
    {
        private readonly Dictionary<string, string> _transformMap = new();

        private readonly RadiusPacketOptions _options = new();

        private string UserPassword => GetString("User-Password");

        public RadiusPacketHeader Header { get; }
        public RadiusAuthenticator Authenticator { get; }

        public IDictionary<string, List<object>> Attributes { get; set; } = new Dictionary<string, List<object>>();
        public SharedSecret SharedSecret { get; }

        public byte[] RequestAuthenticator
        {
            get;
            internal set;
        }

        /// <summary>
        /// EAP session challenge in progress (ie. wpa2-ent)
        /// </summary>
        public bool IsEapMessageChallenge => Header.Code == PacketCode.AccessChallenge && AuthenticationType == AuthenticationType.EAP;

        /// <summary>
        /// ACL and other rules transfer
        /// </summary>
        public bool IsVendorAclRequest => UserName?.StartsWith("#ACSACL#-IP") == true;

        /// <summary>
        /// Is our WinLogon
        /// </summary>
        public bool IsWinLogon => GetString("mfa-client-name") == "WinLogon";

        /// <summary>
        /// OpenVPN with static-challenge sends pwd and otp in base64 with SCRV1 prefix
        /// https://openvpn.net/community-resources/management-interface/
        /// </summary>
        public bool IsOpenVpnStaticChallenge
        {
            get
            {
                var pwd = UserPassword;
                return pwd != null && pwd.StartsWith("SCRV1:");
            }
        }

        public AuthenticationType AuthenticationType
        {
            get
            {
                if (Attributes.ContainsKey("EAP-Message")) return AuthenticationType.EAP;
                if (Attributes.ContainsKey("User-Password")) return AuthenticationType.PAP;
                if (Attributes.ContainsKey("CHAP-Password")) return AuthenticationType.CHAP;
                if (Attributes.ContainsKey("MS-CHAP-Response")) return AuthenticationType.MSCHAP;
                if (Attributes.ContainsKey("MS-CHAP2-Response")) return AuthenticationType.MSCHAP2;

                return AuthenticationType.Unknown;
            }
        }
        public string UserName => _transformMap.ContainsKey("User-Name")
            ? _transformMap["User-Name"]
            : GetString("User-Name");

        //MS RDGW and RRAS
        public string RemoteHostName => GetString("MS-Client-Machine-Account-Name") ?? GetString("MS-RAS-Client-Name");
        public string CallingStationId => GetCallingStationId();
        public string CalledStationId => IsWinLogon ? GetString("Called-Station-Id") : null;
        public string NasIdentifier => GetString("NAS-Identifier");

        public string TryGetUserPassword()
        {
            var password = UserPassword;

            if (IsOpenVpnStaticChallenge)
            {
                try
                {
                    password = password.Split(':')[1].Base64toUtf8();
                }
                catch //invalid packet
                {
                }
            }

            return password;
        }

        /// <summary>
        /// Open VPN static challenge
        /// </summary>
        public string TryGetChallenge()
        {
            var password = UserPassword;

            if (IsOpenVpnStaticChallenge)
            {
                try
                {
                    return password.Split(':')[2].Base64toUtf8();
                }
                catch //invalid packet
                {
                }
            }

            return null;
        }

        public RadiusPacket(RadiusPacketHeader header, RadiusAuthenticator authenticator, SharedSecret sharedSecret)
        {
            Header = header ?? throw new ArgumentNullException(nameof(header));
            Authenticator = authenticator ?? throw new ArgumentNullException(nameof(authenticator));
            SharedSecret = sharedSecret ?? throw new ArgumentNullException(nameof(sharedSecret));
        }

        /// <summary>
        /// Creates a response packet with code, authenticator, identifier and secret from the request packet.
        /// </summary>
        /// <param name="responseCode"></param>
        /// <returns></returns>
        public IRadiusPacket CreateResponsePacket(PacketCode responseCode)
        {
            var header = RadiusPacketHeader.Create(responseCode, Header.Identifier);
            var packet = new RadiusPacket(header, Authenticator, SharedSecret)
            {
                RequestAuthenticator = Authenticator.Value
            };

            return packet;
        }

        public void Configure(Action<RadiusPacketOptions> configure)
        {
            configure?.Invoke(_options);
        }

        /// <summary>
        /// Gets a single attribute value with name cast to type
        /// Throws an exception if multiple attributes with the same name are found
        /// </summary>
        public T GetAttribute<T>(string name)
        {
            if (Attributes.ContainsKey(name))
            {
                return (T)Attributes[name].Single();
            }

            return default;
        }

        /// <summary>
        /// Gets a single string attribute value
        /// Throws an exception if multiple attributes with the same name are found
        /// </summary>
        public string GetString(string name)
        {
            if (!Attributes.ContainsKey(name))
            {
                return null;
            }

            object value;
            try
            {
                value = Attributes[name].Single();
            }
            catch (InvalidOperationException ex)
            {
                throw new Exception("Multiple attributes with the same name are found", ex);
            }

            if (value != null)
            {
                return value switch
                {
                    byte[] _value => Encoding.UTF8.GetString(_value),
                    string _value => _value,
                    _ => value.ToString(),
                };
            }

            return null;
        }

        /// <summary>
        /// Gets multiple attribute values with the same name cast to type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public List<T> GetAttributes<T>(string name)
        {
            if (Attributes.ContainsKey(name))
            {
                return Attributes[name].Cast<T>().ToList();
            }
            return new List<T>();
        }

        public void CopyTo(IRadiusPacket packet)
        {
            packet.Attributes = Attributes;
            packet.Attributes.Remove("Proxy-State"); //should be newer
        }


        public IRadiusPacket Clone()
        {
            var newPacket = new RadiusPacket(Header, Authenticator, SharedSecret);
            newPacket.Attributes = new Dictionary<string, List<Object>>(Attributes);
            newPacket.Attributes.Remove("Proxy-State"); 
            foreach (var attr in _transformMap.Keys)
            {
                newPacket.Attributes.Remove(attr);
                newPacket.AddAttribute(attr, _transformMap[attr]);
            }
            return newPacket;
        }

        public void AddAttribute(string name, string value)
        {
            AddAttributeObject(name, value);
        }

        public void AddAttribute(string name, uint value)
        {
            AddAttributeObject(name, value);
        }

        public void AddAttribute(string name, IPAddress value)
        {
            AddAttributeObject(name, value);
        }

        public void AddAttribute(string name, byte[] value)
        {
            AddAttributeObject(name, value);
        }

        public void AddAttributes(IDictionary<string, object> attributes)
        {
            foreach (var attr in attributes)
            {
                AddAttributeObject(attr.Key, attr.Value);
            }
        }

        public IRadiusPacket UpdateAttribute(string name, string value)
        {
            if (Attributes.ContainsKey(name))
            {
                Attributes.Remove(name);
            }
            AddAttributeObject(name, value);
            return this;
        }

        internal void AddAttributeObject(string name, object value)
        {
            if (!Attributes.ContainsKey(name))
            {
                Attributes.Add(name, new List<object>());
            }
            Attributes[name].Add(value);
        }

        public string CreateUniqueKey(IPEndPoint remoteEndpoint)
        {
            var base64Authenticator = Authenticator.Value.Base64();
            return $"{Header.Code:d}:{Header.Identifier}:{remoteEndpoint}:{UserName}:{base64Authenticator}";
        }

        private string GetCallingStationId()
        {
            if (!string.IsNullOrWhiteSpace(_options.CallingStationIdAttribute))
            {
                return GetString(_options.CallingStationIdAttribute)
                    ?? GetString("Calling-Station-Id")
                    ?? RemoteHostName;
            }
            return GetString("Calling-Station-Id") ?? RemoteHostName;
        }

        public void AddTransformation(string attribute, string transformedValue)
        {
            if (string.IsNullOrWhiteSpace(attribute))
            {
                throw new ArgumentException($"'{nameof(attribute)}' cannot be null or whitespace.", nameof(attribute));
            }

            if (transformedValue is null)
            {
                throw new ArgumentNullException(nameof(transformedValue));
            }

            _transformMap[attribute] = transformedValue;
        }
    }
}