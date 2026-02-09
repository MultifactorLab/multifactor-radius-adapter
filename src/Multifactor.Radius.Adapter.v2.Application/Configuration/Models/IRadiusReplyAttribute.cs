namespace Multifactor.Radius.Adapter.v2.Application.Configuration.Models;

public interface IRadiusReplyAttribute
{
    public string Name { get; }
    public object Value { get; }
    public IReadOnlyList<string> UserGroupCondition { get; }
    public IReadOnlyList<string> UserNameCondition { get; }
    public bool Sufficient { get; }
    public bool IsMemberOf => Name?.ToLower() == "memberof";
    public bool FromLdap => !string.IsNullOrWhiteSpace(Name);
}