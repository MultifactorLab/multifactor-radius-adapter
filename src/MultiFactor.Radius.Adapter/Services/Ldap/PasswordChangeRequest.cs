using System;

namespace MultiFactor.Radius.Adapter.Services.Ldap;

public class PasswordChangeRequest
{
    public string Id { get; private set; } = Guid.NewGuid().ToString();

    public string Domain { get; set; }
    
    public string CurrentPasswordEncryptedData { get; set; }
    
    public string NewPasswordEncryptedData { get; set; }
}