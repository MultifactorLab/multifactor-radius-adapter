using Multifactor.Radius.Adapter.v2.Domain.RadiusAdapter.Sections.UserNameTransform;

namespace Multifactor.Radius.Adapter.v2.Domain.Auth;

public class UserNameTransformRules
{
    public UserNameTransformRule[] BeforeFirstFactor { get; }
    public UserNameTransformRule[] BeforeSecondFactor { get; }

    public UserNameTransformRules(
        IEnumerable<UserNameTransformRule> firstFactorRules,
        IEnumerable<UserNameTransformRule> secondFactorRules)
    {
        ArgumentNullException.ThrowIfNull(firstFactorRules);
        ArgumentNullException.ThrowIfNull(secondFactorRules);

        BeforeFirstFactor = firstFactorRules.ToArray();
        BeforeSecondFactor = secondFactorRules.ToArray();
    }

    public UserNameTransformRules()
    {
        BeforeFirstFactor = [];
        BeforeSecondFactor = [];
    }
}