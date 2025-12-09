using System.Net;
using Multifactor.Radius.Adapter.v2.Domain.Auth;
using Multifactor.Radius.Adapter.v2.Domain.Ldap.Interfaces;
using Multifactor.Radius.Adapter.v2.Domain.MultifactorApi;
using Multifactor.Radius.Adapter.v2.Domain.Radius.Packet;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.MultifactorApi.Dto;

public class SendChallengeRequest
{
    public ApiCredential ApiCredential { get; }
    public ILdapProfile? UserProfile { get; }
    public string? IdentityAttribute { get; }
    public IRadiusPacket RequestPacket { get; }
    public string ConfigName { get; }
    public AuthenticatedClientCacheConfig AuthenticationCacheLifetime { get; }
    public bool BypassSecondFactorWhenApiUnreachable { get; }
    public IPEndPoint RemoteEndpoint { get;  }
    public string Answer { get; }
    public string RequestId { get; }
    public bool ApiResponseCacheEnabled { get; }
    public IReadOnlyList<string> ApiUrls { get; }

    public SendChallengeRequest(RadiusPipelineExecutionContext context, string answer, string requestId, bool cacheEnabled = true)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.ApiCredential);
        ArgumentNullException.ThrowIfNull(context.RequestPacket);
        ArgumentException.ThrowIfNullOrWhiteSpace(context.ClientConfigurationName);
        ArgumentException.ThrowIfNullOrWhiteSpace(answer);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestId);
        ArgumentNullException.ThrowIfNull(context.AuthenticationCacheLifetime);
        ArgumentNullException.ThrowIfNull(context.RemoteEndpoint);
        
        ApiCredential = context.ApiCredential;
        IdentityAttribute = context.LdapServerConfiguration?.IdentityAttribute;
        UserProfile = context.UserLdapProfile;
        RequestPacket = context.RequestPacket;
        ConfigName = context.ClientConfigurationName;
        AuthenticationCacheLifetime = context.AuthenticationCacheLifetime;
        BypassSecondFactorWhenApiUnreachable = context.BypassSecondFactorWhenApiUnreachable;
        RemoteEndpoint = context.RemoteEndpoint;
        Answer = answer;
        RequestId = requestId;
        ApiResponseCacheEnabled = cacheEnabled;
        ApiUrls = context.ApiUrls;
    }
}