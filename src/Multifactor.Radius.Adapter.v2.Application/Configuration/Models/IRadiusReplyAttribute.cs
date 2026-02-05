namespace Multifactor.Radius.Adapter.v2.Application.Configuration.Models;

public interface IRadiusReplyAttribute
{
    public string Name { get; set; }
    public object Value { get; set; }
    public IReadOnlyList<string> UserGroupCondition { get; set; }
    public IReadOnlyList<string> UserNameCondition { get; set; }
    public bool Sufficient { get; set; }
    public bool IsMemberOf => Name?.ToLower() == "memberof";
    public bool FromLdap => !string.IsNullOrWhiteSpace(Name);
}