using System.Net;

namespace Multifactor.Radius.Adapter.v2.Core.Radius.Packet;

public interface IRadiusPacket
{
    public RadiusPacketHeader Header { get; }
    public RadiusAuthenticator Authenticator { get; }
    public RadiusAuthenticator? RequestAuthenticator { get; }
    public AuthenticationType AuthenticationType { get; }
    public string? UserName { get; }
    public bool IsEapMessageChallenge { get; }
    public bool IsVendorAclRequest { get; }
    public bool IsWinLogon { get; }
    public bool IsOpenVpnStaticChallenge { get; }
    public string? MsClientMachineAccountNameAttribute { get; }
    public string? MsRasClientNameAttribute { get; }
    public string? CallingStationIdAttribute { get; }
    public string? CalledStationIdAttribute { get; }
    public string? NasIdentifierAttribute { get; }
    public string? TryGetUserPassword();
    public string? TryGetChallenge();
    public IReadOnlyDictionary<string, RadiusAttribute> Attributes { get; }
    public T GetAttribute<T>(string name);
    public List<T> GetAttributes<T>(string name);
    public string? GetAttributeValueAsString(string name);
    public string CreateUniqueKey(IPEndPoint remoteEndpoint);
}