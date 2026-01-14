namespace Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Models;

public class CheckConnectionRequest
{
    public string ConnectionString { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
    public int BindTimeoutInSeconds { get; set; }
}