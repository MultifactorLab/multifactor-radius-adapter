//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using Microsoft.Extensions.Configuration;
using System;
using System.Linq;

namespace MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models.UserNameTransform;

public class UserNameTransformRulesSection
{
    [ConfigurationKeyName("add")]
    private UserNameTransformRule[] _elements { get; set; }
    
    [ConfigurationKeyName("add")]
    private UserNameTransformRule _singleElement { get; set; }

    public UserNameTransformRule[] Elements
    {
        get
        {
            // To deal with a single element binding to array issue, we should map a single claim manually 
            // See: https://github.com/dotnet/runtime/issues/57325
            if (!string.IsNullOrWhiteSpace(_singleElement?.Match))
            {
                return new [] { _singleElement };
            }
        
            if (_elements != null && _elements.All(x => !string.IsNullOrWhiteSpace(x.Match)))
            {
                return _elements;
            }

            return Array.Empty<UserNameTransformRule>();
        }
    }
}
