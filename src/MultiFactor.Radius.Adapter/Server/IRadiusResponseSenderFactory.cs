//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Configuration.Core;

namespace MultiFactor.Radius.Adapter.Server
{
    public interface IRadiusResponseSenderFactory
    {
        IRadiusResponseSender CreateSender(IUdpClient udpClient);
    }
}