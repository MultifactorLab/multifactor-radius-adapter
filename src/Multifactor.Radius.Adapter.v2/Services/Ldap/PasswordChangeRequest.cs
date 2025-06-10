namespace Multifactor.Radius.Adapter.v2.Services.Ldap;

public class PasswordChangeRequest
{
    public string Id { get; private set; } = Guid.NewGuid().ToString();

    public string Domain { get; set; } = string.Empty;
    
    public string? CurrentPasswordEncryptedData { get; set; }
    
    public string? NewPasswordEncryptedData { get; set; }
}