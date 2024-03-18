//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using System;
using System.Collections.Generic;
using System.Net;
using System.Globalization;
using MultiFactor.Radius.Adapter.Core.Radius.Attributes;
using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Framework.Context;

namespace MultiFactor.Radius.Adapter.Server.Pipeline.PostProcessing;

public class RadiusReplyAttributeEnricher
{
    private readonly IRadiusDictionary _dictionary;
    private readonly ILogger<RadiusReplyAttributeEnricher> _logger;

    public RadiusReplyAttributeEnricher(IRadiusDictionary dictionary, ILogger<RadiusReplyAttributeEnricher> logger)
    {
        _dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void RewriteReplyAttributes(RadiusContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        foreach (var attr in context.ClientConfiguration.RadiusReplyAttributes)
        {
            var breakLoop = false;
            var convertedValues = new List<object>();

            foreach (var attrElement in attr.Value)
            {
                // check condition
                if (!attrElement.IsMatch(context)) continue;

                foreach (var val in attrElement.GetValues(context))
                {
                    _logger.LogDebug("Added/replaced attribute '{attrname:l}:{attrval:l}' to reply", attr.Key, val.ToString());
                    convertedValues.Add(ConvertType(attr.Key, val));
                }

                if (attrElement.Sufficient)
                {
                    breakLoop = true;
                    break;
                }
            }

            context.ResponsePacket.Attributes[attr.Key] = convertedValues;
            if (breakLoop) break;
        }
    }

    private object ConvertType(string attrName, object value)
    {
        if (value is string)
        {
            var stringValue = (string)value;
            var attribute = _dictionary.GetAttribute(attrName);
            switch (attribute.Type)
            {
                case "ipaddr":
                    if (IPAddress.TryParse(stringValue, out var ipValue))
                    {
                        return ipValue;
                    }
                    break;
                case "date":
                    if (DateTime.TryParse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dateValue))
                    {
                        return dateValue;
                    }
                    break;
                case "integer":
                    if (int.TryParse(stringValue, out var integerValue))
                    {
                        return integerValue;
                    }
                    break;
            }
        }

        return value;
    }
}
