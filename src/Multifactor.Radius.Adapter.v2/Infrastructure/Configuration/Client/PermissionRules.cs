using Multifactor.Core.Ldap.LangFeatures;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.Client;

public class PermissionRules : IPermissionRules
{
    /// <summary>
    /// Allowed values
    /// </summary>
    public List<string> IncludedValues { get; }

    /// <summary>
    /// Disallowed values
    /// </summary>
    public List<string> ExcludedValues { get; }

    public PermissionRules(List<string> includedDomains, List<string> excludedDomains)
    {
        Throw.IfNull(includedDomains, nameof(includedDomains));
        Throw.IfNull(excludedDomains, nameof(excludedDomains));
        
        IncludedValues = includedDomains;
        ExcludedValues = excludedDomains;
    }
    
    public PermissionRules()
    {
        IncludedValues = [];
        ExcludedValues = [];
    }

    public bool IsPermitted(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain)) throw new ArgumentNullException(nameof(domain));

        if (IncludedValues.Count > 0)
            return IncludedValues.Any(included => included.Equals(domain, StringComparison.CurrentCultureIgnoreCase));

        if (ExcludedValues.Count > 0)
            return ExcludedValues.All(excluded => !excluded.Equals(domain, StringComparison.CurrentCultureIgnoreCase));
        
        return true;
    }
}