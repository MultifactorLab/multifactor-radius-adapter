using Multifactor.Core.Ldap.LangFeatures;

namespace Multifactor.Radius.Adapter.v2.Core.Configuration.Client;

public class DomainPermissionRules : IDomainPermissionRules
{
    /// <summary>
    /// Use only these domains within forest(s)
    /// </summary>
    public IList<string> IncludedDomains { get; }

    /// <summary>
    /// Use all but not these domains within forest(s)
    /// </summary>
    public IList<string> ExcludedDomains { get; }

    public DomainPermissionRules(IList<string> includedDomains, IList<string> excludedDomains)
    {
        Throw.IfNull(includedDomains, nameof(includedDomains));
        Throw.IfNull(excludedDomains, nameof(excludedDomains));
        
        IncludedDomains = includedDomains;
        ExcludedDomains = excludedDomains;
    }

    public bool IsPermittedDomain(string domain)
    {
        if (string.IsNullOrEmpty(domain)) throw new ArgumentNullException(nameof(domain));

        if (IncludedDomains?.Count > 0)
        {
            return IncludedDomains.Any(included => included.Equals(domain, StringComparison.CurrentCultureIgnoreCase));
        }

        if (ExcludedDomains?.Count > 0)
        {
            return ExcludedDomains.All(excluded => !excluded.Equals(domain, StringComparison.CurrentCultureIgnoreCase));
        }

        return true;
    }
}