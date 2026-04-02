using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Abstractions;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.FirstFactor.Processor;

public static class LdapBindNameFormatter
{
    public static string FormatName(string userName, ILdapProfile ldapProfile)
    {
        var identity = new UserIdentity(userName);
        
        if (identity.Format is UserIdentityFormat.UserPrincipalName or UserIdentityFormat.DistinguishedName)
            return userName;

        return ldapProfile.Dn.StringRepresentation;
    }
}