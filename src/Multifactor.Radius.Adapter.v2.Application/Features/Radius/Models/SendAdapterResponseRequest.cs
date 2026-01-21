using System.Net;
using Multifactor.Core.Ldap.Attributes;
using Multifactor.Radius.Adapter.v2.Application.Configuration.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models.Enum;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;

public class SendAdapterResponseRequest
{
    public bool ShouldSkipResponse { get; set;  }
    public RadiusPacket? ResponsePacket { get; set; }
    public RadiusPacket RequestPacket { get; set; }
    public IPEndPoint RemoteEndpoint { get; set; }
    public IPEndPoint? ProxyEndpoint { get; set; }
    public AuthenticationStatus FirstFactorStatus { get; set; }
    
    public AuthenticationStatus SecondFactorStatus { get; set; }
    public ResponseInformation ResponseInformation { get; set; }
    public SharedSecret RadiusSharedSecret { get; set; }
    public HashSet<string> UserGroups { get; set; }
    public IReadOnlyDictionary<string, RadiusReplyAttribute[]> RadiusReplyAttributes { get; set; }
    public IReadOnlyCollection<LdapAttribute> Attributes { get; set; }
    public (int min, int max)? InvalidCredentialDelay { get; set; }


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