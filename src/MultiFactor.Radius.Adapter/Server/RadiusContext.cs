//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Core.Radius;
using System;
using System.Collections.Generic;
using System.Net;

namespace MultiFactor.Radius.Adapter.Server
{
    public class RadiusContext
    {
        public RadiusContext(IClientConfiguration clientConfiguration, RadiusResponseSender radiusResponseSender, IServiceProvider provider)
        {
            ReceivedAt = DateTime.Now;
            ResponseCode = PacketCode.AccessReject;
            UserGroups = new List<string>();
            ClientConfiguration = clientConfiguration ?? throw new ArgumentNullException(nameof(clientConfiguration));
            ResponseSender = radiusResponseSender ?? throw new ArgumentNullException(nameof(radiusResponseSender));
            RequestServices = provider ?? throw new ArgumentNullException(nameof(provider));
        }
        public IPEndPoint RemoteEndpoint { get; set; }
        public IPEndPoint ProxyEndpoint { get; set; }
        public IRadiusPacket RequestPacket { get; set; }
        public IRadiusPacket ResponsePacket { get; set; }
        public DateTime ReceivedAt { get; set; }
        public PacketCode ResponseCode { get; set; }
        public string State { get; set; }
        public string ReplyMessage { get; set; }
        public string UserName { get; set; }
        public string Upn { get; set; }
        public string DisplayName { get; set; }
        public string UserPhone { get; set; }
        public string EmailAddress { get; set; }
        public bool Bypass2Fa { get; set; }
        public IList<string> UserGroups { get; set; }
        public IDictionary<string, object> LdapAttrs { get; set; }
        public IServiceProvider RequestServices { get; set; }
        public IClientConfiguration ClientConfiguration { get; }
        public RadiusResponseSender ResponseSender { get; }
    }
}
