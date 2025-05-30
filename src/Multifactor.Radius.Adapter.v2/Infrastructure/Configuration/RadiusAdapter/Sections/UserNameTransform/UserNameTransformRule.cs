namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Sections.UserNameTransform;

public class UserNameTransformRule
{
    public string Match { get; init; } = string.Empty;
    public string Replace { get; init; } = string.Empty;
    public int Count { get; init; } = 0;
}
