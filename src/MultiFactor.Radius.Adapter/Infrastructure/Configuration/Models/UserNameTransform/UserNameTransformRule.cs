using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models.UserNameTransformFeature;

namespace MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models.UserNameTransform;

public class UserNameTransformRule
{
    public UserNameTransformRulesElement Element { get; init; }
    public UserNameTransformRulesScope Scope { get; init; }

    public UserNameTransformRule(UserNameTransformRulesElement element, UserNameTransformRulesScope scope)
    {
        Element = element;
        Scope = scope;
    }
}

