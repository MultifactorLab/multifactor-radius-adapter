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

using MultiFactor.Radius.Adapter.Core.Framework.Context;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration;
using System;
using System.Net;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.ClientLevel;

namespace MultiFactor.Radius.Adapter.Server;

public class RadiusContextFactory
{
    private readonly IServiceProvider _serviceProvider;

    public RadiusContextFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public RadiusContext CreateContext(IClientConfiguration client, IRadiusPacket packet, IPEndPoint remote, IPEndPoint proxy)
    {
        if (client is null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        if (packet is null)
        {
            throw new ArgumentNullException(nameof(packet));
        }

        return new RadiusContext(packet, client, _serviceProvider)
        {
            RemoteEndpoint = remote,
            ProxyEndpoint = proxy,
        };
    }
}
