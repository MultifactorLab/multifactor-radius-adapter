namespace Multifactor.Radius.Adapter.v2.Application.Core.Models.Abstractions;

public interface IRadiusReplyAttribute
{
    public string Name { get; }
    public object Value { get; }
    public IReadOnlyList<string> UserGroupCondition { get; }
    public IReadOnlyList<string> UserNameCondition { get; }
    public bool Sufficient { get; }
    public bool IsMemberOf => Name.Equals("memberof", StringComparison.CurrentCultureIgnoreCase);
    public bool FromLdap => !string.IsNullOrWhiteSpace(Name);
}