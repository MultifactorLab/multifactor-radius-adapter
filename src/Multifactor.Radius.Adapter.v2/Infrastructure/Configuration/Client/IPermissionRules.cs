namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.Client;

public interface IPermissionRules
{
    bool IsPermitted(string domain);
    List<string> IncludedValues { get; }
    List<string> ExcludedValues { get; }
}