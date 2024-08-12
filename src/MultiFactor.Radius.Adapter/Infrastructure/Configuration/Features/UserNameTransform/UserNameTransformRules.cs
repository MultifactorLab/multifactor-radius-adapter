using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models.UserNameTransform;
using System.Collections.Generic;
using System.Linq;

namespace MultiFactor.Radius.Adapter.Infrastructure.Configuration.Features.UserNameTransform;

public class UserNameTransformRules
{
    private readonly List<UserNameTransformRule> _rules = new();
    public UserNameTransformRule[] BeforeFirstFactor => _rules.Where(x => x.Scope == UserNameTransformRulesScope.BeforeFirstFactor || x.Scope == UserNameTransformRulesScope.Both).ToArray();

    public UserNameTransformRule[] BeforeSecondFactor => _rules.Where(x => x.Scope == UserNameTransformRulesScope.BeforeSecondFactor || x.Scope == UserNameTransformRulesScope.Both).ToArray();

    public void AddRule(UserNameTransformRulesElement element, UserNameTransformRulesScope scope)
    {
        _rules.Add(new UserNameTransformRule(element, scope));
    }
}
