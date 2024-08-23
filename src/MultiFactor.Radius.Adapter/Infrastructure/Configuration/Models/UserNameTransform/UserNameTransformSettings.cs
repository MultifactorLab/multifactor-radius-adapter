using Microsoft.Extensions.Configuration;

namespace MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models.UserNameTransform;

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
