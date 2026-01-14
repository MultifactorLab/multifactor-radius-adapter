using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Models;

public class LoadSchemaRequest
{
    public string ConnectionString { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
    public int BindTimeoutInSeconds { get; set; }
    
    public static LoadSchemaRequest FromContext(RadiusPipelineContext context)
    {
        return new LoadSchemaRequest
        {
            ConnectionString = context.LdapConfiguration.ConnectionString,
            UserName = context.LdapConfiguration.Username,
            Password = context.LdapConfiguration.Password,
            BindTimeoutInSeconds = context.LdapConfiguration.BindTimeoutSeconds,
        };
    }
}