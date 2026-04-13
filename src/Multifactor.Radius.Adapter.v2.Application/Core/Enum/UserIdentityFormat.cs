namespace Multifactor.Radius.Adapter.v2.Application.Core.Enum;

public enum UserIdentityFormat
{
    Unknown = 0,
    DistinguishedName = 1,
    UserPrincipalName = 2,
    SamAccountName = 3,
    NetBiosName = 4
}