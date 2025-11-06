//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using System.ComponentModel;
using Microsoft.Extensions.Configuration;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Sections.RadiusReply;

[Description("Attributes")]
public class RadiusReplyAttributesSection
{
    [ConfigurationKeyName("add")]
    private RadiusReplyAttribute[] _elements { get; set; }

    [ConfigurationKeyName("add")]
    private RadiusReplyAttribute _singleElement { get; set; }
    
    public RadiusReplyAttributesSection()
    {
    }

    public RadiusReplyAttributesSection(RadiusReplyAttribute singleElement = null, RadiusReplyAttribute[] elements = null)
    {
        _elements = elements;
        _singleElement = singleElement;
    }
    
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
