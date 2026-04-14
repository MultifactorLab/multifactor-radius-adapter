using System.DirectoryServices.Protocols;
using System.Text;
using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Connection.LdapConnectionFactory;
using Multifactor.Core.Ldap.Name;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.SharedServices.ChallengeProcessor.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.SharedServices.ChallengeProcessor.Ports;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.UseCases.ChangePassword;

internal sealed class ChangePassword : IChangePassword
{
    private readonly ILdapConnectionFactory _connectionFactory;

    public ChangePassword(ILdapConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public bool Execute(ChangeUserPasswordDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto, nameof(dto));
        var options = new LdapConnectionOptions(new LdapConnectionString(dto.ConnectionString), 
            dto.AuthType,
            dto.UserName, 
            dto.Password, 
            TimeSpan.FromSeconds(dto.BindTimeoutInSeconds));
        using var connection = _connectionFactory.CreateConnection(options);
        var changePasswordRequest = BuildPasswordChangeRequest(dto.LdapSchema, dto.DistinguishedName, dto.NewPassword);
        var response = connection.SendRequest(changePasswordRequest);
        return response.ResultCode == ResultCode.Success;
    }
    
    private static ModifyRequest BuildPasswordChangeRequest(ILdapSchema ldapSchema, DistinguishedName userDn, string newPassword)
    {
        var attributeName = ldapSchema.LdapServerImplementation == LdapImplementation.ActiveDirectory
            ? "unicodePwd"
            : "userpassword";

        var newPasswordAttribute = new DirectoryAttributeModification
        {
            Name = attributeName,
            Operation = DirectoryAttributeOperation.Replace
        };
        if (ldapSchema.LdapServerImplementation == LdapImplementation.ActiveDirectory)
            newPasswordAttribute.Add(Encoding.Unicode.GetBytes($"\"{newPassword}\""));
        else
            newPasswordAttribute.Add(newPassword);

        return new ModifyRequest(userDn.StringRepresentation, newPasswordAttribute);
    }
}