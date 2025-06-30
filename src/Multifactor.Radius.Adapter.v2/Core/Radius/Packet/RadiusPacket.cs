using System.Net;
using System.Text;

namespace Multifactor.Radius.Adapter.v2.Core.Radius.Packet;

// See https://datatracker.ietf.org/doc/html/rfc2865#section-3 to understand class structure
public class RadiusPacket : IRadiusPacket
{
    private string? UserPassword => GetAttributeValueAsString("User-Password");
    private readonly Dictionary<string, RadiusAttribute> _attributes = new();
    private readonly RadiusPacketHeader _header;
    
    public PacketCode Code => _header.Code;
    public byte Identifier => _header.Identifier;
    public RadiusAuthenticator Authenticator => _header.Authenticator;
    
    /// <summary>
    /// Used for response packets
    /// </summary>
    public RadiusAuthenticator? RequestAuthenticator { get; }

    public IReadOnlyDictionary<string, RadiusAttribute> Attributes => _attributes;

    public string? UserName => GetAttributeValueAsString("User-Name");

    public AuthenticationType AuthenticationType
    {
        get
        {
            if (_attributes.ContainsKey("EAP-Message")) return AuthenticationType.EAP;
            if (_attributes.ContainsKey("User-Password")) return AuthenticationType.PAP;
            if (_attributes.ContainsKey("CHAP-Password")) return AuthenticationType.CHAP;
            if (_attributes.ContainsKey("MS-CHAP-Response")) return AuthenticationType.MSCHAP;
            if (_attributes.ContainsKey("MS-CHAP2-Response")) return AuthenticationType.MSCHAP2;

            return AuthenticationType.Unknown;
        }
    }

    /// <summary>
    /// EAP session challenge in progress (ie. wpa2-ent)
    /// </summary>
    public bool IsEapMessageChallenge => Code == PacketCode.AccessChallenge && AuthenticationType == AuthenticationType.EAP;

    /// <summary>
    /// ACL and other rules transfer
    /// </summary>
    public bool IsVendorAclRequest => UserName?.StartsWith("#ACSACL#-IP") == true;

    /// <summary>
    /// Is our WinLogon
    /// </summary>
    public bool IsWinLogon => GetAttributeValueAsString("mfa-client-name") == "WinLogon";

    /// <summary>
    /// OpenVPN with static-challenge sends pwd and otp in base64 with SCRV1 prefix
    /// https://openvpn.net/community-resources/management-interface/
    /// </summary>
    public bool IsOpenVpnStaticChallenge
    {
        get
        {
            var pwd = UserPassword;
            return pwd != null && pwd.StartsWith("SCRV1:");
        }
    }

    public string? MsClientMachineAccountNameAttribute => GetAttributeValueAsString("MS-Client-Machine-Account-Name");

    public string? MsRasClientNameAttribute => GetAttributeValueAsString("MS-RAS-Client-Name");

    public string? CallingStationIdAttribute => GetAttributeValueAsString("Calling-Station-Id");
    public string? RemoteHostName => GetAttributeValueAsString("MS-Client-Machine-Account-Name") ?? GetAttributeValueAsString("MS-RAS-Client-Name");

    public string? CalledStationIdAttribute => GetAttributeValueAsString("Called-Station-Id");

    public string? NasIdentifierAttribute => GetAttributeValueAsString("NAS-Identifier");
    
    public string? State => GetAttributeValueAsString("State");
    
    public RadiusPacket(RadiusPacketHeader header, RadiusAuthenticator? requestAuthenticator = null)
    {
        _header = header ?? throw new ArgumentNullException(nameof(header));
        RequestAuthenticator = requestAuthenticator;
    }

    public string? TryGetUserPassword()
    {
        var password = UserPassword;

        if (!IsOpenVpnStaticChallenge)
            return password;
        
        var parts = password?.Split(':');
        if ((parts?.Length ?? 0) < 2)
            return null;
            
        password = parts![1].Base64toUtf8();

        return password;
    }

    /// <summary>
    /// Open VPN static challenge
    /// </summary>
    public string? TryGetChallenge()
    {
        var password = UserPassword;

        if (!IsOpenVpnStaticChallenge)
            return null;
        
        var parts = password?.Split(':');
        if ((parts?.Length ?? 0) < 3)
            return null;
        
        return parts![2].Base64toUtf8();
    }

    /// <summary>
    /// Gets a single attribute value with name cast to type
    /// Throws an exception if multiple attributes with the same name are found
    /// </summary>
    public T GetAttribute<T>(string name)
    {
        if (_attributes.ContainsKey(name))
        {
            return (T)_attributes[name].Values.Single();
        }

        return default;
    }

    /// <summary>
    /// Gets multiple attribute values with the same name cast to type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name"></param>
    /// <returns></returns>
    public List<T> GetAttributes<T>(string name)
    {
        if (_attributes.ContainsKey(name))
        {
            return _attributes[name].Values.Cast<T>().ToList();
        }

        return new List<T>();
    }

    /// <summary>
    /// Gets a single string attribute value
    /// Throws an exception if multiple attributes with the same name are found
    /// </summary>
    public string? GetAttributeValueAsString(string name)
    {
        if (!_attributes.ContainsKey(name))
        {
            return null;
        }

        object? value;
        try
        {
            value = _attributes[name].Values.Single();
        }
        catch (InvalidOperationException ex)
        {
            throw new Exception("Multiple attributes with the same name are found", ex);
        }

        if (value != null)
        {
            return value switch
            {
                byte[] val => Encoding.UTF8.GetString(val),
                string val => val,
                _ => value.ToString(),
            };
        }

        return null;
    }

    public string CreateUniqueKey(IPEndPoint remoteEndpoint)
    {
        var base64Authenticator = Authenticator.Value.Base64();
        return $"{Code:d}:{Identifier}:{remoteEndpoint}:{UserName}:{base64Authenticator}";
    }

    public void AddAttributeValue(string name, object? value)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(nameof(name));
        
        if (!_attributes.ContainsKey(name))
            _attributes.Add(name, new RadiusAttribute(name));

        _attributes[name].AddValues(value);
    }

    public void ReplaceAttribute(string name, params object[] values)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(nameof(name));
        
        if (_attributes.ContainsKey(name))
            _attributes.Remove(name);

        var attribute = new RadiusAttribute(name);
        attribute.AddValues(values);
        _attributes.Add(name, attribute);
    }

    public void RemoveAttribute(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(nameof(name));

        _attributes.Remove(name);
    }
}