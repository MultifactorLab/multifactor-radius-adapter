﻿using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Core.Framework.Context;
using System;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Core.Framework.Pipeline;

public class RadiusPipeline
{
    private readonly RadiusRequestDelegate _requestDelegate;
    private readonly IRadiusRequestPostProcessor _postProcessor;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<RadiusPipeline> _logger;

    public RadiusPipeline(RadiusRequestDelegate requestDelegate,
        IRadiusRequestPostProcessor postProcessor,
        IHostEnvironment environment,
        ILogger<RadiusPipeline> logger)
    {
        _requestDelegate = requestDelegate;
        _postProcessor = postProcessor;
        _environment = environment;
        _logger = logger;
    }

    public async Task InvokeAsync(RadiusContext context)
    {
        try
        {
            await _requestDelegate(context);
            await _postProcessor.InvokeAsync(context);
        }
        catch (Exception ex)
        {
            // needed to display the reason for the crash in the test runner
            if (_environment.IsEnvironment("Test"))
            {
                throw new Exception($"Failed to handle radius request: {ex.Message}", ex);
            }
            else
            {
                _logger.LogError(ex, "Failed to handle radius request");
            }
        }
    }
}
