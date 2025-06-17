namespace Multifactor.Radius.Adapter.v2.Core.MultifactorApi;

public class AccessRequest
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
}