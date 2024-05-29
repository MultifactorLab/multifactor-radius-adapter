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
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Core;
using System;
using System.Net;

namespace MultiFactor.Radius.Adapter.Server
{
    public class RadiusResponseSender : IRadiusResponseSender
    {
        private readonly IUdpClient _udpClient;
        private readonly RadiusPacketParser _radiusPacketParser;
        private readonly ILogger _logger;

        public RadiusResponseSender(IUdpClient udpClient, RadiusPacketParser radiusPacketParser, ILogger<RadiusResponseSender> logger)
        {
            _udpClient = udpClient;
            _radiusPacketParser = radiusPacketParser;
            _logger = logger;
        }

        public void Send(IRadiusPacket responsePacket, string user, IPEndPoint remoteEndpoint, IPEndPoint proxyEndpoint, bool debugLog)
        {
            var responseBytes = _radiusPacketParser.GetBytes(responsePacket);
            _udpClient.Send(responseBytes, responseBytes.Length, proxyEndpoint ?? remoteEndpoint);

            if (proxyEndpoint != null)
            {
                if (debugLog)
                {
                    _logger.LogDebug("{code:l} sent to {host:l}:{port} via {proxyhost:l}:{proxyport} id={id}", responsePacket.Header.Code.ToString(), remoteEndpoint.Address, remoteEndpoint.Port, proxyEndpoint.Address, proxyEndpoint.Port, responsePacket.Header.Identifier);
                }
                else
                {
                    _logger.LogInformation("{code:l} sent to {host:l}:{port} via {proxyhost:l}:{proxyport} id={id} user='{user:l}'", responsePacket.Header.Code.ToString(), remoteEndpoint.Address, remoteEndpoint.Port, proxyEndpoint.Address, proxyEndpoint.Port, responsePacket.Header.Identifier, user);
                }
            }
            else
            {
                if (debugLog)
                {
                    _logger.LogDebug("{code:l} sent to {host:l}:{port} id={id}", responsePacket.Header.Code.ToString(), remoteEndpoint.Address, remoteEndpoint.Port, responsePacket.Header.Identifier);
                }
                else
                {
                    _logger.LogInformation("{code:l} sent to {host:l}:{port} id={id} user='{user:l}'", responsePacket.Header.Code.ToString(), remoteEndpoint.Address, remoteEndpoint.Port, responsePacket.Header.Identifier, user);
                }
            }
        }
    }
}