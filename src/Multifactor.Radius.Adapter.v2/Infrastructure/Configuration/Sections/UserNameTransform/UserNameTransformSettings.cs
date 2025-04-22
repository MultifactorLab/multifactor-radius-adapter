using Microsoft.Extensions.Configuration;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.Sections.UserNameTransform;

public class UserNameTransformSettings
{
    [ConfigurationKeyName("add")]
    private UserNameTransformRule[] _elements { get; set; }

    public UserNameTransformRule[] Elements
    {
        get
        {
            return _elements;
        }
    }
}
