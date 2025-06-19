namespace Multifactor.Radius.Adapter.v2.Core.MultifactorApi;

public class AccessRequestResponse
{
    public string Id { get; set; } = string.Empty;
    public string Identity { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public RequestStatus Status { get; set; }
    public string ReplyMessage { get; set; } = string.Empty;
    public bool Bypassed { get; set; }
    public string Authenticator { get; set; } = string.Empty;
    public string AuthenticatorId { get; set; } = string.Empty;
    public string Account { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;

    public static AccessRequestResponse Bypass => new() { Status = RequestStatus.Granted, Bypassed = true };
}