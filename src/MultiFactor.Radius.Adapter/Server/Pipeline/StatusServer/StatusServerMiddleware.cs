//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Core;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Framework.Context;
using MultiFactor.Radius.Adapter.Framework.Pipeline;
using System;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Server.Pipeline.StatusServer;

public class StatusServerMiddleware : IRadiusMiddleware
{
    private readonly ApplicationVariables _variables;

    public StatusServerMiddleware(ApplicationVariables variables)
    {
        _variables = variables;
    }

    public async Task InvokeAsync(RadiusContext context, RadiusRequestDelegate next)
    {
        if (context.RequestPacket.Header.Code != PacketCode.StatusServer)
        {
            await next(context);
            return;
        }

        var uptime = _variables.UpTime;
        context.SetReplyMessage($"Server up {uptime.Days} days {uptime.ToString("hh\\:mm\\:ss")}, ver.: {_variables.AppVersion}");
        context.Authentication.Accept();
    }
}