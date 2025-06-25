using System.Net;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Core;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.Ldap;
using Multifactor.Radius.Adapter.v2.Core.Pipeline;
using Multifactor.Radius.Adapter.v2.Core.Pipeline.Settings;
using Multifactor.Radius.Adapter.v2.Core.Radius.Packet;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;

public interface IRadiusPipelineExecutionContext
{
    IPipelineExecutionSettings Settings { get; }
    ILdapProfile UserLdapProfile { get; set; }
    IRadiusPacket RequestPacket { get; }
    IRadiusPacket? ResponsePacket { get; set; }
    IAuthenticationState AuthenticationState { get; set; }
    IResponseInformation ResponseInformation { get; set; }
    IExecutionState ExecutionState { get; }
    string? MustChangePasswordDomain { get; set; }
    IPEndPoint RemoteEndpoint { get; set; }
    IPEndPoint? ProxyEndpoint { get; set; }
    ILdapSchema? LdapSchema { get; set; }
    UserPassphrase Passphrase { get; set; }
}