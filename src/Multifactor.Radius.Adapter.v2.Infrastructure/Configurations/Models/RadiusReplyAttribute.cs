using Multifactor.Radius.Adapter.v2.Application.Core.Models.Abstractions;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Models;

internal sealed class RadiusReplyAttribute : IRadiusReplyAttribute
{
    public string Name { get; set; } = string.Empty;
    public object Value { get; set; } = string.Empty;
    public IReadOnlyList<string> UserGroupCondition { get; set; } = [];
    public IReadOnlyList<string> UserNameCondition { get; set; } = [];
    public bool Sufficient { get; set; }
    public bool IsMemberOf => Name?.ToLower() == "memberof";
    public bool FromLdap => !string.IsNullOrWhiteSpace(Name);
}
