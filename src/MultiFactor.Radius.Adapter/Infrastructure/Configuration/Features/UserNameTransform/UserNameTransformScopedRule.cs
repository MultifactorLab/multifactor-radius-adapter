using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models.UserNameTransform;

namespace MultiFactor.Radius.Adapter.Infrastructure.Configuration.Features.UserNameTransform;

public class UserNameTransformScopedRule
{
    public UserNameTransformRule Element { get; init; }
    public UserNameTransformRuleKind Kind { get; init; }

    public UserNameTransformScopedRule(UserNameTransformRule element, UserNameTransformRuleKind kind)
    {
        Element = element;
        Kind = kind;
    }
}

