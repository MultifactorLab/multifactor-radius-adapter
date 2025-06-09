using System.Net;
using Multifactor.Radius.Adapter.v2.Core;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.Ldap;
using Multifactor.Radius.Adapter.v2.Core.Pipeline;
using Multifactor.Radius.Adapter.v2.Core.Pipeline.Settings;
using Multifactor.Radius.Adapter.v2.Core.Radius.Packet;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;

public interface IRadiusPipelineExecutionContext
{
    IPipelineExecutionSettings Settings { get; }
    ILdapProfile UserLdapProfile { get; }
    IRadiusPacket RequestPacket { get; }
    IRadiusPacket ResponsePacket { get; set; }
    IAuthenticationState AuthenticationState { get;  }
    IResponseInformation ResponseInformation { get; }
    IExecutionState ExecutionState { get; }
    string MustChangePasswordDomain { get; set; }
    public IPEndPoint RemoteEndpoint { get; set; }
    public IPEndPoint ProxyEndpoint { get; set; }
    public ILdapServerConfiguration FirstFactorLdapServerConfiguration { get; set; }
    public UserPassphrase Passphrase { get; set; }
}