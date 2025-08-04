namespace Multifactor.Radius.Adapter.v2.Core.Configuration.Client;

public interface IPermissionRules
{
    bool IsPermitted(string domain);
    List<string> IncludedValues { get; }
    List<string> ExcludedValues { get; }
}