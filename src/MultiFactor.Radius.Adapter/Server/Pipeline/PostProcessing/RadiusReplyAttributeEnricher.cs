﻿//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Core.Framework.Context;
using MultiFactor.Radius.Adapter.Core.Radius.Attributes;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Net;

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

        foreach (var attr in context.Configuration.RadiusReplyAttributes)
        {
            var breakLoop = false;
            var convertedValues = new List<object>();

            foreach (var attrElement in attr.Value)
            {
                // check condition
                if (!attrElement.IsMatch(context)) continue;

                foreach (var val in attrElement.GetValues(context))
                {
                    if(val == null)
                    {
                        _logger.LogDebug("Attribute '{attrname:l}' got no value, skipping", attr.Key);
                        continue;
                    }
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
        if (value is string stringValue)
        {
            var attribute = _dictionary.GetAttribute(attrName);
            switch (attribute.Type)
            {
                case "ipaddr":
                    if (IPAddress.TryParse(stringValue, out var ipValue))
                    {
                        return ipValue;
                    }
                    
                    // maybe it is msRADIUSFramedIPAddress value
                    if (int.TryParse(stringValue, out var val))
                    {
                        return MsRadiusFramedIpAddressToIpAddress(val);
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
    
    private IPAddress MsRadiusFramedIpAddressToIpAddress(int intValue)
    {
        long longValue = intValue;
            
        // Microsoft subtracts 4294967296 from numbers above 2147483647 to
        // make them negative to make it, sort of, unsigned.
        // https://document.phenixid.net/m/90910/l/1601121-how-to-setup-framed-ip-using-ad-with-msradiusframedipaddress-attribute
        if (longValue < 0)
        {
            longValue += 4294967296;
        }
            
        var bytes = BitConverter.GetBytes(longValue).Take(4).ToArray();
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        return new IPAddress(bytes);
    }
}
