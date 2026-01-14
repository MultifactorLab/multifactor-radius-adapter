namespace Multifactor.Radius.Adapter.v2.Application.Configuration.Models;

public class RadiusReplyAttribute
{
    public string Name { get; init; } = string.Empty;
    public object Value { get; init; } = string.Empty;
    public IReadOnlyList<string> UserGroupCondition { get; set; } = [];
    public IReadOnlyList<string> UserNameCondition { get; set; } = [];
    public bool Sufficient { get; init; }
    public bool IsMemberOf => Name?.ToLower() == "memberof";
    public bool FromLdap => !string.IsNullOrWhiteSpace(Name);
}
