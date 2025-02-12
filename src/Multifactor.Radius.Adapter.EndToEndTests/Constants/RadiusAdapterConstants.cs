namespace Multifactor.Radius.Adapter.EndToEndTests.Constants;

internal static class RadiusAdapterConstants
{
    public const string LocalHost = "127.0.0.1";
    public const int DefaultRadiusAdapterPort = 1812;
    public const string DefaultSharedSecret = "000";
    public const string DefaultNasIdentifier = "e2e";
    
    public const string BindUserName = "BindUser";
    public const string BindUserPassword = "Qwerty123!";
    
    public const string AdminUserName = "e2eAdmin";
    public const string AdminUserPassword = "Qwerty123!";
    
    public const string ChangePasswordUserName = "PasswordUser";
    public const string ChangePasswordUserPassword = "Qwerty123!";
}