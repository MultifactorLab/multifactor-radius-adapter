using Microsoft.Extensions.Configuration;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models.UserNameTransformFeature;

namespace MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models.UserNameTransform;

public class UserNameTransformSettings
{
    [ConfigurationKeyName("add")]
    private UserNameTransformRulesElement[] _elements { get; set; }

    public UserNameTransformRulesElement[] Elements
    {
        get
        {
            return _elements;
        }
    }
}
