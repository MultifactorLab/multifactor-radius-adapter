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
using System.Net;
using System.Threading.Tasks;
using System.Net.Sockets;
using MultiFactor.Radius.Adapter.Configuration.Core;
using System.Runtime.CompilerServices;

namespace MultiFactor.Radius.Adapter.Server
{
    internal class RealUdpClient : IUdpClient
    {
        private readonly UdpClient _udpClient;

        public RealUdpClient(IPEndPoint endpoint)
        {
            if (endpoint is null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            _udpClient = new UdpClient(endpoint);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Close() => _udpClient.Close();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<UdpReceiveResult> ReceiveAsync() => _udpClient.ReceiveAsync();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Send(byte[] dgram, int bytes, IPEndPoint endPoint) => _udpClient.Send(dgram, bytes, endPoint);
    }
}