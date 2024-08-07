using Microsoft.Extensions.Configuration;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models.RadiusReply;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models.UserNameTransformFeature;
using System;
using System.ComponentModel;
using System.Linq;

namespace MultiFactor.Radius.Adapter.Infrastructure.Configuration.Features.UserNameTransformFeature
{
    [Description("Attributes")]
    public class UserNameTransformRulesCollection
    {
        [ConfigurationKeyName("add")]
        private UserNameTransformRulesElement[] _elements { get; set; }
        [ConfigurationKeyName("add")]
        private UserNameTransformRulesElement _singleElement { get; set; }

        public UserNameTransformRulesElement[] Elements
        {
            get
            {
                // To deal with a single element binding to array issue, we should map a single claim manually 
                // See: https://github.com/dotnet/runtime/issues/57325
                var hasSingle = !string.IsNullOrWhiteSpace(_singleElement?.Match) ||
                                !string.IsNullOrWhiteSpace(_singleElement?.Replace);
                if (hasSingle)
                {
                    return new[] { _singleElement };
                }

                if (_elements != null)
                {
                    return _elements;
                }

                return Array.Empty<UserNameTransformRulesElement>();
            }
        }
    }
}