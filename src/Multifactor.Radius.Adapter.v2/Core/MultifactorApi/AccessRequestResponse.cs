namespace Multifactor.Radius.Adapter.v2.Core.MultifactorApi;

public class AccessRequestResponse
{
    public string Id { get; set; }
    public string Identity { get; set; }
    public string Phone { get; set; }
    public RequestStatus Status { get; set; }
    public string ReplyMessage { get; set; }
    public bool Bypassed { get; set; }
    public string Authenticator { get; set; }
    public string AuthenticatorId { get; set; }
    public string Account { get; set; }
    public string CountryCode { get; set; }
    public string Region { get; set; }
    public string City { get; set; }

    public static AccessRequestResponse Bypass
    {
        get
        {
            return new AccessRequestResponse { Status = RequestStatus.Granted, Bypassed = true };
        }
    }
}