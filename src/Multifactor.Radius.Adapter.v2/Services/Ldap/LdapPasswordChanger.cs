using System.DirectoryServices.Protocols;
using System.Text;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Name;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Core.Ldap;

namespace Multifactor.Radius.Adapter.v2.Services.Ldap;

public class LdapPasswordChanger : ILdapPasswordChanger
{
    private readonly ILdapConnection _ldapConnection;
    private readonly ILdapSchema _ldapSchema;

    public LdapPasswordChanger(ILdapConnection ldapConnection, ILdapSchema ldapSchema)
    {
        ArgumentNullException.ThrowIfNull(ldapConnection, nameof(ldapConnection));
        ArgumentNullException.ThrowIfNull(ldapSchema, nameof(ldapSchema));

        _ldapConnection = ldapConnection;
        _ldapSchema = ldapSchema;
    }

    public Task<PasswordChangeResponse> ChangeUserPasswordAsync(string newPassword, ILdapProfile? profile)
    {
        try
        {
            if (profile is null)
                return Task.FromResult(new PasswordChangeResponse() { Success = false, Message = "No user profile. Cannot change password." });

            var userDn = profile.Dn;
            var request = BuildPasswordChangeRequest(userDn, newPassword);
            var response = _ldapConnection.SendRequest(request);
            if (response.ResultCode != ResultCode.Success)
                return Task.FromResult(new PasswordChangeResponse() { Success = false, Message = response.ErrorMessage });

            return Task.FromResult(new PasswordChangeResponse() { Success = true });
        }
        catch (Exception e)
        {
            return Task.FromResult(new PasswordChangeResponse() { Success = false, Message = e.Message });
        }
    }

    private ModifyRequest BuildPasswordChangeRequest(DistinguishedName userDn, string newPassword)
    {
        var attributeName = _ldapSchema.LdapServerImplementation == LdapImplementation.ActiveDirectory
            ? "unicodePwd"
            : "userpassword";

        var newPasswordAttribute = new DirectoryAttributeModification()
        {
            Name = attributeName,
            Operation = DirectoryAttributeOperation.Replace
        };
        if (_ldapSchema.LdapServerImplementation == LdapImplementation.ActiveDirectory)
            newPasswordAttribute.Add(Encoding.Unicode.GetBytes($"\"{newPassword}\""));
        else
            newPasswordAttribute.Add(newPassword);

        return new ModifyRequest(userDn.StringRepresentation, newPasswordAttribute);
    }
}