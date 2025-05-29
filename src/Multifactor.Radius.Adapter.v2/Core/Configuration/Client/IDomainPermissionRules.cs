namespace Multifactor.Radius.Adapter.v2.Core.Configuration.Client;

public interface IDomainPermissionRules
{
    bool IsPermittedDomain(string domain);
}