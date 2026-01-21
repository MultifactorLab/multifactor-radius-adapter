using Multifactor.Radius.Adapter.v2.Application.Features.Multifactor.Models;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Adapters.Multifactor.Models;

public class AccessRequestDto
{
    public string? Identity { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? PassCode { get; set; }
    public string? CallingStationId { get; set; }
    public string? CalledStationId { get; set; }
    public Capabilities? Capabilities { get; set; }
    public GroupPolicyPreset? GroupPolicyPreset { get; set; }

    public static AccessRequestDto FromQuery(AccessRequestQuery query)
    {
        return new AccessRequestDto
        {
            Identity = query.Identity,
            Name = query.Name,
            Email = query.Email,
            Phone = query.Phone,
            PassCode = query.PassCode,
            CalledStationId = query.CalledStationId,
            Capabilities = new Capabilities{ InlineEnroll = query.InlineEnroll },
            GroupPolicyPreset = new GroupPolicyPreset{ SignUpGroups = query.SignUpGroups }
        };
    }
}

public class Capabilities
{
    public bool InlineEnroll { get; set; }
}
public class GroupPolicyPreset
{
    public string SignUpGroups { get; set; }
}
