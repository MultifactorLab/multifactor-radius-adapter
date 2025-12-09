namespace Multifactor.Radius.Adapter.v2.Domain.MultifactorApi.Interfaces;

public interface IMultifactorApi
{
    Task<AccessRequestResponse> CreateAccessRequest(string address, AccessRequest payload, ApiCredential apiCredentials);
    Task<AccessRequestResponse> SendChallengeAsync(string address, ChallengeRequest payload, ApiCredential apiCredentials);
}