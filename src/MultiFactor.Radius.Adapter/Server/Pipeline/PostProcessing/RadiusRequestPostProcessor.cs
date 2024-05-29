//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Core.Framework.Context;
using MultiFactor.Radius.Adapter.Core.Framework.Pipeline;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration;
using MultiFactor.Radius.Adapter.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Server.Pipeline.PostProcessing;

public class RadiusRequestPostProcessor : IRadiusRequestPostProcessor
{
    private readonly IServiceConfiguration _serviceConfiguration;
    private readonly RadiusReplyAttributeEnricher _attributeEnricher;
    private readonly IRadiusResponseSender _sender;
    private readonly ILogger _logger;

    public RadiusRequestPostProcessor(IServiceConfiguration serviceConfiguration,
        RadiusReplyAttributeEnricher attributeRewriter,
        IRadiusResponseSender sender,
        ILogger<RadiusRequestPostProcessor> logger)
    {
        _serviceConfiguration = serviceConfiguration;
        _attributeEnricher = attributeRewriter;
        _sender = sender;
        _logger = logger;
    }

    public async Task InvokeAsync(RadiusContext context)
    {
        if (context.Flags.SkipResponseFlag)
        {
            return; //stop processing
        }

        if (context.ResponsePacket?.IsEapMessageChallenge == true)
        {
            //EAP authentication in process, just proxy response
            _logger.LogDebug("Proxying EAP-Message Challenge to {host:l}:{port} id={id}", context.RemoteEndpoint.Address, context.RemoteEndpoint.Port, context.RequestPacket.Header.Identifier);
            _sender.Send(context.ResponsePacket, context.RequestPacket?.UserName, context.RemoteEndpoint, context.ProxyEndpoint, true);

            return; //stop processing
        }

        if (context.RequestPacket.IsVendorAclRequest && context.ResponsePacket != null)
        {
            //ACL and other rules transfer, just proxy response
            _logger.LogDebug("Proxying #ACSACL# to {host:l}:{port} id={id}", context.RemoteEndpoint.Address, context.RemoteEndpoint.Port, context.RequestPacket.Header.Identifier);
            _sender.Send(context.ResponsePacket, context.RequestPacket?.UserName, context.RemoteEndpoint, context.ProxyEndpoint, true);

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
                else
                {
                    context.ResponsePacket = responsePacket;
                }

                if (context.RequestPacket.Header.Code == PacketCode.StatusServer)
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
                    if (context.ResponsePacket.Header.Code == PacketCode.AccessReject) //for mschap pwd change only
                    {
                        context.ResponsePacket.CopyTo(responsePacket);
                    }
                }
                await new RandomWaiter(context.Configuration.InvalidCredentialDelay).WaitSomeTimeAsync();
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

        var debugLog = context.RequestPacket.Header.Code == PacketCode.StatusServer;
        _sender.Send(responsePacket, context.RequestPacket?.UserName, context.RemoteEndpoint, context.ProxyEndpoint, debugLog);
    }
}