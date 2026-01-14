namespace Multifactor.Radius.Adapter.v2.Application.Features.AccessChallenge.Models;

public class PersonalData
{
    public string Identity { get; set; }
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? CallingStationId { get; set; }
    public string? CalledStationId { get; set; }
}