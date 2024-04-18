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

using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Core;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Core.Radius.Attributes;
using MultiFactor.Radius.Adapter.Framework.Context;
using MultiFactor.Radius.Adapter.Framework.Pipeline;
using MultiFactor.Radius.Adapter.Logging;
using MultiFactor.Radius.Adapter.Services;
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Server
{
    public sealed class RadiusServer : IDisposable
    {
        private readonly IUdpClient _udpClient;
        private readonly RadiusPacketParser _radiusPacketParser;
        private readonly IRadiusDictionary _dictionary;
        private int _concurrentHandlerCount = 0;
        private readonly ILogger _logger;
        private readonly RadiusPipeline _pipeline;
        private readonly RadiusContextFactory _radiusContextFactory;
        private readonly ApplicationVariables _variables;
        private readonly IServiceConfiguration _serviceConfiguration;

        private readonly CacheService _cacheService;

        private bool _running;

        /// <summary>
        /// Create a new server on endpoint with packet handler repository
        /// </summary>
        public RadiusServer(IServiceConfiguration serviceConfiguration, 
            IUdpClient udpClient,
            IRadiusDictionary dictionary, 
            RadiusPacketParser radiusPacketParser, 
            CacheService cacheService, 
            ILogger<RadiusServer> logger,
            RadiusPipeline pipeline,
            RadiusContextFactory radiusContextFactory,
            ApplicationVariables variables)
        {
            _serviceConfiguration = serviceConfiguration ?? throw new ArgumentNullException(nameof(serviceConfiguration));
            _dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
            _radiusPacketParser = radiusPacketParser ?? throw new ArgumentNullException(nameof(radiusPacketParser));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            _radiusContextFactory = radiusContextFactory ?? throw new ArgumentNullException(nameof(radiusContextFactory));
            _udpClient = udpClient;
            _variables = variables;
        }

        /// <summary>
        /// Start listening for requests
        /// </summary>
        public void Start()
        {
            if (_running)
            {
                _logger.LogWarning("Radius server already started");
                return;
            }

            _logger.LogInformation("Multifactor (c) RADIUS Adapter, v. {Version:l}", _variables.AppVersion);
            _logger.LogInformation("Starting Radius server on {host:l}:{port}", 
                _serviceConfiguration.ServiceServerEndpoint.Address,
                _serviceConfiguration.ServiceServerEndpoint.Port);

            _logger.LogInformation(_dictionary.GetInfo());

            _running = true;
            var receiveTask = Receive();

            _logger.LogInformation("Radius server started");           
        }

        /// <summary>
        /// Stop listening
        /// </summary>
        public void Stop()
        {
            if (!_running)
            {
                _logger.LogWarning("Radius server already stopped");
                return;
            }
                     
            _logger.LogInformation("Stopping radius server");
            _running = false;
            _udpClient?.Close();
            _logger.LogInformation("Radius server stopped");          
        }

        /// <summary>
        /// Start the loop used for receiving packets
        /// </summary>
        /// <returns></returns>
        private async Task Receive()
        {
            while (_running)
            {
                try
                {
                    var response = await _udpClient.ReceiveAsync();
                    var task = Task.Factory.StartNew(() => HandlePacket(response.RemoteEndPoint, response.Buffer), TaskCreationOptions.LongRunning);
                }
                catch (ObjectDisposedException) { } // This is thrown when udpclient is disposed, can be safely ignored
                catch (Exception ex)
                {
                    _logger.LogError("Something went wrong transmitting packet: {message:l}", ex.Message);
                }
            }
        }

        /// <summary>
        /// Used to handle the packets asynchronously
        /// </summary>
        private void HandlePacket(IPEndPoint remoteEndpoint, byte[] packetBytes)
        {
            try
            {
                var handlersCount = Interlocked.Increment(ref _concurrentHandlerCount);
                _logger.LogTrace("Received packet from {host:l}:{port}, Concurrent handlers count: {handlersCount}", remoteEndpoint.Address, remoteEndpoint.Port, handlersCount);
                ParseAndProcess(packetBytes, remoteEndpoint);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is OverflowException)
            {
                _logger.LogWarning(ex, "Ignoring malformed(?) packet received from {host}:{port}", remoteEndpoint.Address, remoteEndpoint.Port);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to receive packet from {host:l}:{port}", remoteEndpoint.Address, remoteEndpoint.Port);
            }
            finally
            {
                Interlocked.Decrement(ref _concurrentHandlerCount);
            }
        }

        /// <summary>
        /// Parses a packet and gets a response packet from the handler
        /// </summary>
        internal void ParseAndProcess(byte[] packetBytes, IPEndPoint remoteEndpoint)
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
            clientConfiguration ??= _serviceConfiguration.GetClient(remoteEndpoint.Address);

            if (clientConfiguration == null)
            {
                _logger.LogWarning("Received packet from unknown client {host:l}:{port}, ignoring", remoteEndpoint.Address, remoteEndpoint.Port);
                return;
            }

            var secret = new SharedSecret(clientConfiguration.RadiusSharedSecret);
            var requestPacket = _radiusPacketParser.Parse(packetBytes, secret,
              configure: x => x.CallingStationIdAttribute = clientConfiguration.CallingStationIdVendorAttribute);

            var context = _radiusContextFactory.CreateContext(clientConfiguration, requestPacket, remoteEndpoint, proxyEndpoint);
            LoggerScope.Wrap(context => HandleRequest(context), context);          
        }

        private void HandleRequest(RadiusContext context)
        {
            var packet = context.RequestPacket;
            var isRetransmission = _cacheService.IsRetransmission(packet, context.RemoteEndpoint);
            if (isRetransmission)
            {
                _logger.LogDebug("Retransmissed request from {host:l}:{port} id={id} client '{client:l}', ignoring",
                    context.RemoteEndpoint.Address,
                    context.RemoteEndpoint.Port,
                    packet.Header.Identifier,
                    context.Configuration.Name);
                return;
            }

            if (context.ProxyEndpoint != null)
            {
                if (packet.Header.Code == PacketCode.StatusServer)
                {
                    _logger.LogInformation("Received {code:l} from {host:l}:{port} proxied by {proxyhost:l}:{proxyport} id={id} client '{client:l}'", 
                    packet.Header.Code.ToString(),
                    context.RemoteEndpoint.Address,
                    context.RemoteEndpoint.Port,
                    context.ProxyEndpoint.Address,
                    context.ProxyEndpoint.Port,
                    packet.Header.Identifier,
                    context.Configuration.Name);
                }
                else
                {
                    _logger.LogInformation("Received {code:l} from {host:l}:{port} proxied by {proxyhost:l}:{proxyport} id={id} user='{user:l}' client '{client:l}'", 
                        packet.Header.Code.ToString(),
                        context.RemoteEndpoint.Address,
                        context.RemoteEndpoint.Port,
                        context.ProxyEndpoint.Address,
                        context.ProxyEndpoint.Port,
                        packet.Header.Identifier,
                        packet.UserName,
                        context.Configuration.Name);
                }
            }
            else
            {
                if (packet.Header.Code == PacketCode.StatusServer)
                {
                    _logger.LogDebug("Received {code:l} from {host:l}:{port} id={id} client '{client:l}'",
                        packet.Header.Code.ToString(), 
                        context.RemoteEndpoint.Address, 
                        context.RemoteEndpoint.Port,
                        packet.Header.Identifier,
                        context.Configuration.Name);
                }
                else
                {
                    _logger.LogInformation("Received {code:l} from {host:l}:{port} id={id} user='{user:l}' client '{client:l}'",
                        packet.Header.Code.ToString(), 
                        context.RemoteEndpoint.Address, 
                        context.RemoteEndpoint.Port,
                        packet.Header.Identifier,
                        packet.UserName, 
                        context.Configuration.Name);
                }
            }

            Task.Run(async () =>
            {
                await _pipeline.InvokeAsync(context);
            });
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

            if (proxySig != "PROXY")
            {
                return false;
            }

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

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            _udpClient?.Close();
        }
    }
}