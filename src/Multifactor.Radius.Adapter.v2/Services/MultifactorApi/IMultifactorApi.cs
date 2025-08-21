using Multifactor.Radius.Adapter.v2.Core.MultifactorApi;

namespace Multifactor.Radius.Adapter.v2.Services.MultifactorApi;

public interface IMultifactorApi
{
    Task<AccessRequestResponse> CreateAccessRequest(string address, AccessRequest payload, ApiCredential apiCredentials);
    Task<AccessRequestResponse> SendChallengeAsync(string address, ChallengeRequest payload, ApiCredential apiCredentials);
}