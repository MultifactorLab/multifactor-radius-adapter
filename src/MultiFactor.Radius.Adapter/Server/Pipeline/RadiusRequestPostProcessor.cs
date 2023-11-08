//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Core.Pipeline;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Services;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections;
using MultiFactor.Radius.Adapter.Core.Radius.Attributes;
using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Configuration;

namespace MultiFactor.Radius.Adapter.Server.Pipeline;

public class RadiusRequestPostProcessor : IRadiusRequestPostProcessor
{
    private readonly IServiceConfiguration _serviceConfiguration;
    private readonly RandomWaiter _waiter;
    private readonly RadiusReplyAttributeEnricher _attributeEnricher;
    private readonly ILogger _logger;

    public RadiusRequestPostProcessor(IServiceConfiguration serviceConfiguration, 
        RandomWaiter waiter, 
        RadiusReplyAttributeEnricher attributeRewriter,
        ILogger<RadiusRequestPostProcessor> logger)
    {
        _serviceConfiguration = serviceConfiguration ?? throw new ArgumentNullException(nameof(serviceConfiguration));
        _waiter = waiter ?? throw new ArgumentNullException(nameof(waiter));
        _attributeEnricher = attributeRewriter ?? throw new ArgumentNullException(nameof(attributeRewriter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(RadiusContext context)
    {
        if (context.ResponseCode == PacketCode.DisconnectNak)
        {
            return; //stop processing
        }

        if (context.ResponsePacket?.IsEapMessageChallenge == true)
        {
            //EAP authentication in process, just proxy response
            _logger.LogDebug("Proxying EAP-Message Challenge to {host:l}:{port} id={id}", context.RemoteEndpoint.Address, context.RemoteEndpoint.Port, context.RequestPacket.Identifier);
            context.ResponseSender.Send(context.ResponsePacket, context.RequestPacket?.UserName, context.RemoteEndpoint, context.ProxyEndpoint, true);

            return; //stop processing
        }

        if (context.RequestPacket.IsVendorAclRequest && context.ResponsePacket != null)
        { 
            //ACL and other rules transfer, just proxy response
            _logger.LogDebug("Proxying #ACSACL# to {host:l}:{port} id={id}", context.RemoteEndpoint.Address, context.RemoteEndpoint.Port, context.RequestPacket.Identifier);
            context.ResponseSender.Send(context.ResponsePacket, context.RequestPacket?.UserName, context.RemoteEndpoint, context.ProxyEndpoint, true);

            return; //stop processing
        }

        var requestPacket = context.RequestPacket;
        var responsePacket = requestPacket.CreateResponsePacket(context.ResponseCode);

        switch (context.ResponseCode)
        {
            case PacketCode.AccessAccept:
                if (context.ResponsePacket != null) //copy from remote radius reply
                {
                    context.ResponsePacket.CopyTo(responsePacket);
                }
                if (context.RequestPacket.Code == PacketCode.StatusServer)
                {
                    responsePacket.AddAttribute("Reply-Message", context.ReplyMessage);
                }

                var clientConfiguration = _serviceConfiguration.GetClient(context);
                //add custom reply attributes
                if (context.ResponseCode == PacketCode.AccessAccept)
                {
                    _attributeEnricher.RewriteReplyAttributes(context);
                }

                break;
            case PacketCode.AccessChallenge:
                responsePacket.AddAttribute("Reply-Message", context.ReplyMessage ?? "Enter OTP code: ");
                responsePacket.AddAttribute("State", context.State); //state to match user authentication session

                break;
            case PacketCode.AccessReject:
                if (context.ResponsePacket != null) //copy from remote radius reply
                {
                    if (context.ResponsePacket.Code == PacketCode.AccessReject) //for mschap pwd change only
                    {
                        context.ResponsePacket.CopyTo(responsePacket);
                    }
                }
                await _waiter.WaitSomeTimeAsync();
                break;
            default:
                throw new NotImplementedException(context.ResponseCode.ToString());
        }


        //proxy echo required
        if (requestPacket.Attributes.ContainsKey("Proxy-State"))
        {
            if (!responsePacket.Attributes.ContainsKey("Proxy-State"))
            {
                responsePacket.Attributes.Add("Proxy-State", requestPacket.Attributes.SingleOrDefault(o => o.Key == "Proxy-State").Value);
            }
        }

        var debugLog = context.RequestPacket.Code == PacketCode.StatusServer;
        context.ResponseSender.Send(responsePacket, context.RequestPacket?.UserName, context.RemoteEndpoint, context.ProxyEndpoint, debugLog);
    }
}