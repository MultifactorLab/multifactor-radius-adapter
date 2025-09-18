namespace Multifactor.Radius.Adapter.v2.Core.FirstFactor.LdapAuth;

public class AuthResult
{
    public bool IsSuccess { get; set; }
    
    public string ErrorMessage { get; set; } = string.Empty;
}