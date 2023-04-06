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

using Serilog;
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MultiFactor.Radius.Adapter.Services;
using MultiFactor.Radius.Adapter.Logging;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Configuration.Core;

namespace MultiFactor.Radius.Adapter.Server
{
    public class PacketProcessor
    {
        private readonly IServiceConfiguration _serviceConfiguration;
        private readonly IRadiusPacketParser _radiusPacketParser;
        private readonly RadiusRouter _router;
        private readonly CacheService _cacheService;
        private readonly ILogger _logger;

        public PacketProcessor(IServiceConfiguration serviceConfiguration, IRadiusPacketParser radiusPacketParser, 
            RadiusRouter router, CacheService cacheService, 
            ILogger logger)
        {
            _serviceConfiguration = serviceConfiguration;
            _radiusPacketParser = radiusPacketParser;
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Parses a packet and gets a response packet from the handler
        /// </summary>
        public void Process(byte[] packetBytes, IPEndPoint remoteEndpoint)
        {
            IPEndPoint proxyEndpoint = null;

            if (IsProxyProtocol(packetBytes, out var sourceEndpoint, out var requestWithoutProxyHeader))
            {
                packetBytes = requestWithoutProxyHeader;
                proxyEndpoint = remoteEndpoint;
                remoteEndpoint = sourceEndpoint;
            }

            IClientConfiguration clientConfiguration = null;
            if (RadiusPacketNasIdentifierParser.TryParse(packetBytes, out var nasIdentifier))
            {
                clientConfiguration = _serviceConfiguration.GetClient(nasIdentifier);
            }
            if (clientConfiguration == null)
            {
                clientConfiguration = _serviceConfiguration.GetClient(remoteEndpoint.Address);
            }

            if (clientConfiguration == null)
            {
                _logger.Warning("Received packet from unknown client {host:l}:{port}, ignoring", remoteEndpoint.Address, remoteEndpoint.Port);
                return;
            }

            var requestPacket = _radiusPacketParser.Parse(packetBytes, Encoding.UTF8.GetBytes(clientConfiguration.RadiusSharedSecret),
                configure: x => x.CallingStationIdAttribute = clientConfiguration.CallingStationIdVendorAttribute);
            var requestScope = new RequestScope(clientConfiguration, remoteEndpoint, proxyEndpoint, requestPacket);
            LoggerScope.Wrap(HandleRequest, requestScope);
        }

        private bool IsProxyProtocol(byte[] request, out IPEndPoint sourceEndpoint, out byte[] requestWithoutProxyHeader)
        {
            //https://www.haproxy.org/download/1.8/doc/proxy-protocol.txt

            sourceEndpoint = null;
            requestWithoutProxyHeader = null;

            if (request.Length < 6)
            {
                return false;
            }

            var proxySig = Encoding.ASCII.GetString(request.Take(5).ToArray());

            if (proxySig == "PROXY")
            {
                var lf = Array.IndexOf(request, (byte)'\n');
                var headerBytes = request.Take(lf + 1).ToArray();
                var header = Encoding.ASCII.GetString(headerBytes);

                var parts = header.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                var sourceIp = parts[2];
                var sourcePort = int.Parse(parts[4]);

                sourceEndpoint = new IPEndPoint(IPAddress.Parse(sourceIp), sourcePort);
                requestWithoutProxyHeader = request.Skip(lf + 1).ToArray();

                return true;
            }

            return false;
        }

        private void HandleRequest(RequestScope requestScope)
        {
            var isRetransmission = _cacheService.IsRetransmission(requestScope.Packet, requestScope.RemoteEndpoint);
            if (isRetransmission)
            {
                _logger.Debug("Retransmissed request from {host:l}:{port} id={id} client '{client:l}', ignoring",
                    requestScope.RemoteEndpoint.Address,
                    requestScope.RemoteEndpoint.Port,
                    requestScope.Packet.Identifier,
                    requestScope.ClientConfiguration.Name);
                return;
            }

            if (requestScope.ProxyEndpoint != null)
            {
                if (requestScope.Packet.Code == PacketCode.StatusServer)
                {
                    _logger.Information("Received {code:l} from {host:l}:{port} proxied by {proxyhost:l}:{proxyport} id={id} client '{client:l}'",
                    requestScope.Packet.Code.ToString(),
                    requestScope.RemoteEndpoint.Address,
                    requestScope.RemoteEndpoint.Port,
                    requestScope.ProxyEndpoint.Address,
                    requestScope.ProxyEndpoint.Port,
                    requestScope.Packet.Identifier,
                    requestScope.ClientConfiguration.Name);
                }
                else
                {
                    _logger.Information("Received {code:l} from {host:l}:{port} proxied by {proxyhost:l}:{proxyport} id={id} user='{user:l}' client '{client:l}'",
                        requestScope.Packet.Code.ToString(),
                        requestScope.RemoteEndpoint.Address,
                        requestScope.RemoteEndpoint.Port,
                        requestScope.ProxyEndpoint.Address,
                        requestScope.ProxyEndpoint.Port,
                        requestScope.Packet.Identifier,
                        requestScope.Packet.UserName,
                        requestScope.ClientConfiguration.Name);
                }
            }
            else
            {
                if (requestScope.Packet.Code == PacketCode.StatusServer)
                {
                    _logger.Debug("Received {code:l} from {host:l}:{port} id={id} client '{client:l}'",
                        requestScope.Packet.Code.ToString(),
                        requestScope.RemoteEndpoint.Address,
                        requestScope.RemoteEndpoint.Port,
                        requestScope.Packet.Identifier,
                        requestScope.ClientConfiguration.Name);
                }
                else
                {
                    _logger.Information("Received {code:l} from {host:l}:{port} id={id} user='{user:l}' client '{client:l}'",
                        requestScope.Packet.Code.ToString(),
                        requestScope.RemoteEndpoint.Address,
                        requestScope.RemoteEndpoint.Port,
                        requestScope.Packet.Identifier,
                        requestScope.Packet.UserName,
                        requestScope.ClientConfiguration.Name);
                }
            }


            var request = new PendingRequest
            {
                RemoteEndpoint = requestScope.RemoteEndpoint,
                ProxyEndpoint = requestScope.ProxyEndpoint,
                RequestPacket = requestScope.Packet,
                UserName = requestScope.Packet.UserName
            };

            Task.Run(async () => await _router.HandleRequest(request, requestScope.ClientConfiguration));
        }

    }
}