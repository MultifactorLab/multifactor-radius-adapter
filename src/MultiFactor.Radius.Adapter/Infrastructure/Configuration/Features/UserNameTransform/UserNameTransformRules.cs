using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models.UserNameTransform;
using System.Collections.Generic;
using System.Linq;

namespace MultiFactor.Radius.Adapter.Infrastructure.Configuration.Features.UserNameTransform;

public class UserNameTransformRules
{
    private readonly List<UserNameTransformScopedRule> _rules = new();
    public UserNameTransformScopedRule[] BeforeFirstFactor => _rules.Where(x => x.Kind == UserNameTransformRuleKind.BeforeFirstFactor || x.Kind == UserNameTransformRuleKind.Both).ToArray();

    public UserNameTransformScopedRule[] BeforeSecondFactor => _rules.Where(x => x.Kind == UserNameTransformRuleKind.BeforeSecondFactor || x.Kind == UserNameTransformRuleKind.Both).ToArray();

    public void AddRule(UserNameTransformRule element, UserNameTransformRuleKind kind)
    {
        _rules.Add(new UserNameTransformScopedRule(element, kind));
    }
}
