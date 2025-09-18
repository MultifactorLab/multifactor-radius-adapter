using System.Text;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.FirstFactor.MsChapV2;
using Multifactor.Radius.Adapter.v2.Core.Radius;
using Multifactor.Radius.Adapter.v2.Core.Radius.Packet;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;
using Multifactor.Radius.Adapter.v2.Services.Ldap;

namespace Multifactor.Radius.Adapter.v2.Core.FirstFactor.LdapAuth.MsChapV2;

public class MsChapV2AuthProcessor : ILdapAuthProcessor
{
    private readonly ILdapProfileService _ldapProfileService;
    private readonly ILogger<MsChapV2AuthProcessor> _logger;

    public AuthenticationType AuthenticationType => AuthenticationType.MSCHAP2;

    public MsChapV2AuthProcessor(ILdapProfileService ldapProfileService, ILogger<MsChapV2AuthProcessor> logger)
    {
        _ldapProfileService = ldapProfileService;
        _logger = logger;
    }
    
    public async Task<AuthResult> Auth(IRadiusPipelineExecutionContext context)
    {
        var radiusPacket = context.RequestPacket;
        var userName = radiusPacket.UserName;
        
        if (string.IsNullOrWhiteSpace(userName))
        {
            _logger.LogWarning("Can't find User-Name in message id={id} from {host:l}:{port}", radiusPacket.Identifier, context.RemoteEndpoint.Address, context.RemoteEndpoint.Port);
            return new AuthResult() { IsSuccess = false };;
            
        }
        var challenge = radiusPacket.GetAttribute<byte[]>("MS-CHAP-Challenge");
        var response = radiusPacket.GetAttribute<byte[]>("MS-CHAP2-Response");

        var pwdBytes = await GetUserPassword(context); //maybe md4 
        var password = Encoding.ASCII.GetString(pwdBytes);
        
        if (challenge.Length != 16)
        {
            _logger.LogWarning("MS-CHAP-Challenge length is incorrect.");
            return new AuthResult() { IsSuccess = false };
        }
        
        if (response.Length != 50)
        {
            _logger.LogWarning("MS-CHAP2-Response length is incorrect.");
            return new AuthResult() { IsSuccess = false };
        }

        var ident = response[0];
        var peerChallenge = response[2..18];
        var peerResponse = response[26..50];
        var ntResponse = Rfc2759.GenerateNTResponse(challenge, peerChallenge, userName, password);

        if (!ntResponse.SequenceEqual(peerResponse))
        {
            _logger.LogWarning("Calculated NT-Response is not equal to peer response.");
            return new AuthResult() { IsSuccess = false };
        }

        var recvKey = Rfc3079.MakeKey(ntResponse, password, false);

        var sendKey = Rfc3079.MakeKey(ntResponse, password, true);

        // MS-CHAP2-Success calculation
        var authenticatorResponse = Rfc2759.GenerateAuthenticatorResponse(challenge, peerChallenge, ntResponse, userName, password);
        var success = new List<byte>(43);
        success.Add(ident);
        var authenticatorResponseBytes = Encoding.ASCII.GetBytes(authenticatorResponse);
        success.AddRange(authenticatorResponseBytes);
        context.ResponseInformation.Attributes.Add("MS-CHAP2-Success", CreateAttribute("MS-CHAP2-Success", success));
        
        // MS-MPPE-Recv-Key calculation
        var recvKeyAttr = GetMsMPPEKey(context.RequestPacket, recvKey, context.RadiusSharedSecret.Bytes);
        context.ResponseInformation.Attributes.Add("MS-MPPE-Recv-Key", CreateAttribute("MS-MPPE-Recv-Key", recvKeyAttr));
        
        // MS-MPPE-Send-Key calculation
        var sendKeyAttr = GetMsMPPEKey(context.RequestPacket, sendKey, context.RadiusSharedSecret.Bytes);
        context.ResponseInformation.Attributes.Add("MS-MPPE-Send-Key", CreateAttribute("MS-MPPE-Send-Key", sendKeyAttr));
        
        context.ResponseInformation.Attributes.Add("MS-MPPE-Encryption-Policy", CreateAttribute("MS-MPPE-Encryption-Policy", 1));
        context.ResponseInformation.Attributes.Add("MS-MPPE-Encryption-Types", CreateAttribute("MS-MPPE-Encryption-Types", 6));
        
        context.AuthenticationState.FirstFactorStatus = AuthenticationStatus.Accept;

        return new AuthResult() { IsSuccess = true };
    }

    private async Task<byte[]> GetUserPassword(IRadiusPipelineExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context.UserLdapProfile);
        ArgumentNullException.ThrowIfNull(context.LdapServerConfiguration);
        ArgumentNullException.ThrowIfNull(context.LdapSchema);
        
        return await _ldapProfileService.GetUserPassword(new GetUserPasswordRequest(context.UserLdapProfile, context.LdapServerConfiguration, context.LdapSchema));
    }

    private RadiusAttribute CreateAttribute(string attributeName, params object[] value)
    {
        var attribute = new RadiusAttribute(attributeName);
        attribute.AddValues(value);
        return attribute;
    }

    private byte[] GetMsMPPEKey(IRadiusPacket requestPacket, byte[] key, byte[] secret)
    {
        var salt = Rfc2868.GenerateSalt();
        var tunnelPassword = Rfc2868.NewTunnelPassword(key, salt, secret, requestPacket.Authenticator.Value);
        return tunnelPassword;
    }
}