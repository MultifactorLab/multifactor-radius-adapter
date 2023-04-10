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
using System.Net;
using System.Net.Sockets;
using MultiFactor.Radius.Adapter.Core.Radius;

namespace MultiFactor.Radius.Adapter.Server
{
    public class RadiusResponseSender : IRadiusResponseSender
    {
        private readonly UdpClient _udpClient;
        private readonly IRadiusPacketParser _radiusPacketParser;
        private readonly ILogger _logger;

        public RadiusResponseSender(UdpClient udpClient, IRadiusPacketParser radiusPacketParser, ILogger logger)
        {
            _udpClient = udpClient ?? throw new ArgumentNullException(nameof(udpClient));
            _radiusPacketParser = radiusPacketParser ?? throw new ArgumentNullException(nameof(radiusPacketParser));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Send(IRadiusPacket responsePacket, string user, IPEndPoint remoteEndpoint, IPEndPoint proxyEndpoint, bool debugLog)
        {
            var responseBytes = _radiusPacketParser.GetBytes(responsePacket);
            _udpClient.Send(responseBytes, responseBytes.Length, proxyEndpoint ?? remoteEndpoint);

            if (proxyEndpoint != null)
            {
                if (debugLog)
                {
                    _logger.Debug("{code:l} sent to {host:l}:{port} via {proxyhost:l}:{proxyport} id={id}", responsePacket.Code.ToString(), remoteEndpoint.Address, remoteEndpoint.Port, proxyEndpoint.Address, proxyEndpoint.Port, responsePacket.Identifier);
                }
                else
                {
                    _logger.Information("{code:l} sent to {host:l}:{port} via {proxyhost:l}:{proxyport} id={id} user='{user:l}'", responsePacket.Code.ToString(), remoteEndpoint.Address, remoteEndpoint.Port, proxyEndpoint.Address, proxyEndpoint.Port, responsePacket.Identifier, user);
                }
            }
            else
            {
                if (debugLog)
                {
                    _logger.Debug("{code:l} sent to {host:l}:{port} id={id}", responsePacket.Code.ToString(), remoteEndpoint.Address, remoteEndpoint.Port, responsePacket.Identifier);
                }
                else
                {
                    _logger.Information("{code:l} sent to {host:l}:{port} id={id} user='{user:l}'", responsePacket.Code.ToString(), remoteEndpoint.Address, remoteEndpoint.Port, responsePacket.Identifier, user);
                }
            }
        }
    }
}