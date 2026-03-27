using Multifactor.Core.Ldap.Schema;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps.FirstFactor.BindNameFormat;

public class LdapBindNameFormatterProvider : ILdapBindNameFormatterProvider
{
    private readonly List<ILdapBindNameFormatter> _formatters = [];
    
    public LdapBindNameFormatterProvider(IEnumerable<ILdapBindNameFormatter> formatters)
    {
        _formatters.AddRange(formatters);
    }

    public ILdapBindNameFormatter? GetLdapBindNameFormatter(LdapImplementation ldapImplementation)
    {
        return _formatters.FirstOrDefault(f => f.LdapImplementation == ldapImplementation);
    }
}