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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace MultiFactor.Radius.Adapter.Core.Radius
{
    /// <summary>
    /// This class encapsulates a Radius packet and presents it in a more readable form
    /// </summary>
    public class RadiusPacket : IRadiusPacket
    {
        private readonly RadiusPacketOptions _options = new RadiusPacketOptions();

        public PacketCode Code
        {
            get;
            internal set;
        }
        public byte Identifier
        {
            get;
            set;
        }
        public byte[] Authenticator { get; set; } = new byte[16];
        public IDictionary<string, List<object>> Attributes { get; set; } = new Dictionary<string, List<object>>();
        public byte[] SharedSecret
        {
            get;
            internal set;
        }
        public byte[] RequestAuthenticator
        {
            get;
            internal set;
        }
        /// <summary>
        /// EAP session challenge in progress (ie. wpa2-ent)
        /// </summary>
        public bool IsEapMessageChallenge
        {
            get
            {
                return Code == PacketCode.AccessChallenge && AuthenticationType == AuthenticationType.EAP;
            }
        }
        /// <summary>
        /// ACL and other rules transfer
        /// </summary>
        public bool IsVendorAclRequest
        {
            get
            {
                return UserName?.StartsWith("#ACSACL#-IP") == true;
            }
        }
        /// <summary>
        /// Is our WinLogon
        /// </summary>
        public bool IsWinLogon
        {
            get
            {
                return GetString("mfa-client-name") == "WinLogon";
            }
        }
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
        public string UserName => GetString("User-Name");
        internal string UserPassword => GetString("User-Password");
        public string RemoteHostName
        {
            get
            {
                //MS RDGW and RRAS
                return GetString("MS-Client-Machine-Account-Name") ?? GetString("MS-RAS-Client-Name");
            }
        }
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

        internal RadiusPacket()
        {
        }


        /// <summary>
        /// Create a new packet with a random authenticator
        /// </summary>
        /// <param name="code"></param>
        /// <param name="identifier"></param>
        /// <param name="secret"></param>
        /// <param name="authenticator">Set authenticator for testing</param>
        public RadiusPacket(PacketCode code, byte identifier, string secret)
        {
            Code = code;
            Identifier = identifier;
            SharedSecret = Encoding.UTF8.GetBytes(secret);

            // Generate random authenticator for access request packets
            if (Code == PacketCode.AccessRequest || Code == PacketCode.StatusServer)
            {
                using (var csp = RandomNumberGenerator.Create())
                {
                    csp.GetNonZeroBytes(Authenticator);
                }
            }

            // A Message authenticator is required in status server packets, calculated last
            if (Code == PacketCode.StatusServer)
            {
                AddAttribute("Message-Authenticator", new byte[16]);
            }
        }


        /// <summary>
        /// Creates a response packet with code, authenticator, identifier and secret from the request packet.
        /// </summary>
        /// <param name="responseCode"></param>
        /// <returns></returns>
        public IRadiusPacket CreateResponsePacket(PacketCode responseCode)
        {
            return new RadiusPacket
            {
                Code = responseCode,
                SharedSecret = SharedSecret,
                Identifier = Identifier,
                RequestAuthenticator = Authenticator
            };
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
            if (Attributes.ContainsKey(name))
            {
                var value = Attributes[name].Single();

                if (value != null)
                {
                    switch (value)
                    {
                        case byte[] _value:
                            return Encoding.UTF8.GetString(_value);
                        case string _value:
                            return _value;
                        default:
                            return value.ToString();
                    }
                }
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
            var base64Authenticator = Authenticator.Base64();
            return $"{Code.ToString("d")}:{Identifier}:{remoteEndpoint}:{UserName}:{base64Authenticator}";
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
    }
}