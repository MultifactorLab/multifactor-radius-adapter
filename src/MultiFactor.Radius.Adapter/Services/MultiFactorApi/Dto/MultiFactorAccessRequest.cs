//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

namespace MultiFactor.Radius.Adapter.Services.MultiFactorApi.Dto;

public class AccessRequestDto
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

    public static AccessRequestDto Bypass
    {
        get
        {
            return new AccessRequestDto { Status = RequestStatus.Granted, Bypassed = true };
        }
    }
}
