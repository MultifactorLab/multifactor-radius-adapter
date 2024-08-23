using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models.UserNameTransform;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiFactor.Radius.Adapter.Infrastructure.Configuration.Features.UserNameTransform;

public class UserNameTransformRules
{
    private readonly UserNameTransformRule[] _firstFactorRules;
    public UserNameTransformRule[] BeforeFirstFactor => _firstFactorRules;

    private readonly UserNameTransformRule[] _secondFactorRules;
    public UserNameTransformRule[] BeforeSecondFactor => _secondFactorRules;

    public UserNameTransformRules(IEnumerable<UserNameTransformRule> firstFactorRules, IEnumerable<UserNameTransformRule> secondFactorRules)
    {
        _firstFactorRules = firstFactorRules.ToArray();
        _secondFactorRules = secondFactorRules.ToArray();
    }

    public UserNameTransformRules()
    {
        _firstFactorRules = Array.Empty<UserNameTransformRule>();
        _secondFactorRules = Array.Empty<UserNameTransformRule>();
    }
}
