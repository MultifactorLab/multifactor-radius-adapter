using System.Net;
using Multifactor.Core.Ldap.Attributes;
using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Abstractions;

namespace Multifactor.Radius.Adapter.v2.Application.Core.Models;

public sealed class SendAdapterResponseRequest
{
    public bool ShouldSkipResponse { get; init;  }
    public required RadiusPacket RequestPacket { get; init; }
    public RadiusPacket? ResponsePacket { get; init; }
    public IPEndPoint RemoteEndpoint { get; init; }
    public IPEndPoint? ProxyEndpoint { get; init; }
    public AuthenticationStatus FirstFactorStatus { get; init; }
    
    public AuthenticationStatus SecondFactorStatus { get; init; }
    public ResponseInformation ResponseInformation { get; init; }
    public SharedSecret RadiusSharedSecret { get; init; }
    public HashSet<string> UserGroups { get; init; }
    public IReadOnlyDictionary<string, IReadOnlyList<IRadiusReplyAttribute>> RadiusReplyAttributes { get; private init; }
    public IReadOnlyCollection<LdapAttribute> Attributes { get; private init; }
    public CredentialDelay? InvalidCredentialDelay { get; private init; }


    public static SendAdapterResponseRequest FromContext(RadiusPipelineContext context)
    {
        return new SendAdapterResponseRequest
        {
            ShouldSkipResponse = context.ShouldSkipResponse,
            ResponsePacket = context.ResponsePacket,
            RequestPacket = context.RequestPacket,
            RemoteEndpoint = context.RequestPacket.RemoteEndpoint,
            ProxyEndpoint = context.RequestPacket.ProxyEndpoint,
            FirstFactorStatus = context.FirstFactorStatus,
            SecondFactorStatus = context.SecondFactorStatus,
            ResponseInformation = context.ResponseInformation,
            RadiusSharedSecret = new SharedSecret(context.ClientConfiguration.RadiusSharedSecret),
            UserGroups = context.UserGroups,
            RadiusReplyAttributes = context.ClientConfiguration.ReplyAttributes,
            Attributes = context.LdapProfile?.Attributes ?? [],
            InvalidCredentialDelay = context.ClientConfiguration.InvalidCredentialDelay
        };
    }
}