//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using Microsoft.Extensions.Configuration;
using System;
using System.ComponentModel;
using System.Linq;

namespace MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models.RadiusReply;

[Description("Attributes")]
public class RadiusReplyAttributesSection
{
    [ConfigurationKeyName("add")]
    private RadiusReplyAttribute[] _elements { get; set; }

    [ConfigurationKeyName("add")]
    private RadiusReplyAttribute _singleElement { get; set; }

    public RadiusReplyAttribute[] Elements
    {
        get
        {
            // To deal with a single element binding to array issue, we should map a single claim manually 
            // See: https://github.com/dotnet/runtime/issues/57325
            if (!string.IsNullOrWhiteSpace(_singleElement?.Name))
            {
                return new [] { _singleElement };
            }
        
            if (_elements != null && _elements.All(x => !string.IsNullOrWhiteSpace(x.Name)))
            {
                return _elements;
            }

            return Array.Empty<RadiusReplyAttribute>();
        }
    }
}
