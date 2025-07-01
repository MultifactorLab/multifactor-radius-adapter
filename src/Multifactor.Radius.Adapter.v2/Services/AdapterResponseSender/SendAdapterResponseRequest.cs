using System.Net;
using Multifactor.Core.Ldap.Attributes;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.Pipeline;
using Multifactor.Radius.Adapter.v2.Core.Radius;
using Multifactor.Radius.Adapter.v2.Core.Radius.Packet;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;

namespace Multifactor.Radius.Adapter.v2.Services.AdapterResponseSender;

public class SendAdapterResponseRequest
{
    public bool ShouldSkipResponse { get; }
    public IRadiusPacket? ResponsePacket { get; }
    public IRadiusPacket RequestPacket { get; }
    public IPEndPoint RemoteEndpoint { get; }
    public IPEndPoint? ProxyEndpoint { get; }
    public IAuthenticationState AuthenticationState { get; }
    public IResponseInformation ResponseInformation { get; }
    public SharedSecret RadiusSharedSecret { get; }
    public HashSet<string> UserGroups { get; }
    public IReadOnlyDictionary<string, RadiusReplyAttributeValue[]> RadiusReplyAttributes { get; }
    public IReadOnlyCollection<LdapAttribute> Attributes { get; }

    public SendAdapterResponseRequest(IRadiusPipelineExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.RequestPacket);
        ArgumentNullException.ThrowIfNull(context.RemoteEndpoint);
        ArgumentNullException.ThrowIfNull(context.AuthenticationState);
        ArgumentNullException.ThrowIfNull(context.ResponseInformation);
        ArgumentNullException.ThrowIfNull(context.RadiusSharedSecret);
        ArgumentNullException.ThrowIfNull(context.UserGroups);
        ArgumentNullException.ThrowIfNull(context.RadiusReplyAttributes);
        ArgumentNullException.ThrowIfNull(context.UserLdapProfile?.Attributes);
        
        ShouldSkipResponse = context.ExecutionState.ShouldSkipResponse;
        ResponsePacket = context.ResponsePacket;
        RequestPacket = context.RequestPacket;
        RemoteEndpoint = context.RemoteEndpoint;
        ProxyEndpoint = context.ProxyEndpoint;
        AuthenticationState = context.AuthenticationState;
        ResponseInformation = context.ResponseInformation;
        RadiusSharedSecret = context.RadiusSharedSecret;
        UserGroups = context.UserGroups;
        RadiusReplyAttributes = context.RadiusReplyAttributes;
        Attributes = context.UserLdapProfile.Attributes;
    }
}