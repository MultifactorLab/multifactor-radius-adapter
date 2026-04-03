using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SecondFactor.Multifactor.Models;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.UseCases.SecondFactor.Models;

internal sealed class AccessRequestQuery
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

    public static AccessRequestQuery FromAppModel(AccessRequestDto query)
    {
        return new AccessRequestQuery
        {
            Identity = query.Identity,
            Name = query.Name,
            Email = query.Email,
            Phone = query.Phone,
            PassCode = query.PassCode,
            CalledStationId = query.CalledStationId,
            CallingStationId = query.CallingStationId,
            Capabilities = new Capabilities(true),
            GroupPolicyPreset = new GroupPolicyPreset(query.SignUpGroups)
        };
    }
}

internal sealed record Capabilities(bool InlineEnroll);
internal sealed record GroupPolicyPreset(string? SignUpGroups);
