//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Core.Radius;
using System.Net;

namespace MultiFactor.Radius.Adapter.Server
{
    public interface IRadiusResponseSender
    {
        void Send(IRadiusPacket responsePacket, string user, IPEndPoint remoteEndpoint, IPEndPoint proxyEndpoint, bool debugLog);
    }
}