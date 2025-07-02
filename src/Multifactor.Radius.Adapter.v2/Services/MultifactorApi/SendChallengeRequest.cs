using System.Net;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.Ldap;
using Multifactor.Radius.Adapter.v2.Core.MultifactorApi;
using Multifactor.Radius.Adapter.v2.Core.Radius.Packet;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;

namespace Multifactor.Radius.Adapter.v2.Services.MultifactorApi;

public class SendChallengeRequest
{
    public ApiCredential ApiCredential { get; }
    public ILdapProfile UserProfile { get; }
    public string? IdentityAttribute { get; }
    public IRadiusPacket RequestPacket { get; }
    public string ConfigName { get; }
    public AuthenticatedClientCacheConfig AuthenticationCacheLifetime { get; }
    public bool BypassSecondFactorWhenApiUnreachable { get; }
    public IPEndPoint RemoteEndpoint { get;  }
    public string Answer { get; }
    public string RequestId { get; }

    public SendChallengeRequest(IRadiusPipelineExecutionContext context, string answer, string requestId)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.ApiCredential);
        ArgumentNullException.ThrowIfNull(context.LdapServerConfiguration);
        ArgumentNullException.ThrowIfNull(context.UserLdapProfile);
        ArgumentNullException.ThrowIfNull(context.RequestPacket);
        ArgumentException.ThrowIfNullOrWhiteSpace(context.ClientConfigurationName);
        ArgumentException.ThrowIfNullOrWhiteSpace(answer);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestId);
        ArgumentNullException.ThrowIfNull(context.AuthenticationCacheLifetime);
        ArgumentNullException.ThrowIfNull(context.RemoteEndpoint);
        
        ApiCredential = context.ApiCredential;
        IdentityAttribute = context.LdapServerConfiguration.IdentityAttribute;
        UserProfile = context.UserLdapProfile;
        RequestPacket = context.RequestPacket;
        ConfigName = context.ClientConfigurationName;
        AuthenticationCacheLifetime = context.AuthenticationCacheLifetime;
        BypassSecondFactorWhenApiUnreachable = context.BypassSecondFactorWhenApiUnreachable;
        RemoteEndpoint = context.RemoteEndpoint;
        Answer = answer;
        RequestId = requestId;
    }
}