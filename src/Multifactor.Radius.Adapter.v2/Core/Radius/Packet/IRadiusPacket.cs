using System.Net;

namespace Multifactor.Radius.Adapter.v2.Core.Radius.Packet;

public interface IRadiusPacket
{
    PacketCode Code { get; }
    byte Identifier { get; }
    RadiusAuthenticator Authenticator { get; }
    RadiusAuthenticator? RequestAuthenticator { get; }
    AuthenticationType AuthenticationType { get; }
    string? UserName { get; }
    bool IsEapMessageChallenge { get; }
    bool IsVendorAclRequest { get; }
    bool IsWinLogon { get; }
    bool IsOpenVpnStaticChallenge { get; }
    string? MsClientMachineAccountNameAttribute { get; }
    string? MsRasClientNameAttribute { get; }
    string? CallingStationIdAttribute { get; }
    string? RemoteHostName { get; }
    string? CalledStationIdAttribute { get; }
    string? NasIdentifierAttribute { get; }
    string? State { get; }
    string? TryGetUserPassword();
    string? TryGetChallenge();
    IReadOnlyDictionary<string, RadiusAttribute> Attributes { get; }
    T GetAttribute<T>(string name);
    List<T> GetAttributes<T>(string name);
    string? GetAttributeValueAsString(string name);
    string CreateUniqueKey(IPEndPoint remoteEndpoint);
    AccountType AccountType { get; }
}