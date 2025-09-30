using System.DirectoryServices.Protocols;
using Multifactor.Core.Ldap.Entry;
using Multifactor.Core.Ldap.Name;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Core.Ldap;

namespace Multifactor.Radius.Adapter.v2.Services.Ldap.Forest;

public class ActiveDirectoryLdapForestLoader : ILdapForestLoader
{
    private readonly string _domainLocation = "cn=System";
    private readonly string _domainObjectClass = "trustedDomain";
    private readonly string _suffixLocation = "cn=Partitions,cn=Configuration";
    private readonly string _suffixAttribute = "uPNSuffixes";
    
    public LdapImplementation LdapImplementation => LdapImplementation.ActiveDirectory;
    
    public IEnumerable<DistinguishedName> LoadTrustedDomains(ILdapConnection connection, ILdapSchema schema)
    {
        var trustedDomainsResult = connection.Find(
            new DistinguishedName($"{_domainLocation},{schema.NamingContext.StringRepresentation}"),
            $"{schema.ObjectClass}={_domainObjectClass}",
            SearchScope.OneLevel,
            attributes: schema.Cn);

        var trustedDomains = trustedDomainsResult
            .Select(x => GetAttributeValue(x, schema.Cn))
            .Where(x => x.Count != 0)
            .SelectMany(x => x)
            .Select(LdapNamesUtils.FqdnToDn);

        return trustedDomains;
    }

    public IEnumerable<string> LoadDomainSuffixes(ILdapConnection connection, ILdapSchema schema)
    {
        var upnSuffixesResult = connection.Find(
            new DistinguishedName($"{_suffixLocation},{schema.NamingContext.StringRepresentation}"),
            $"{schema.ObjectClass}=*",
            SearchScope.Base,
            attributes: _suffixAttribute);

        var upnSuffixes = upnSuffixesResult
            .Select(x => GetAttributeValue(x, _suffixAttribute))
            .Where(x => x.Count != 0)
            .SelectMany(x => x);

        return upnSuffixes;
    }
    
    private IReadOnlyCollection<string> GetAttributeValue(LdapEntry entry, string attributeName)
    {
        var attribute = entry.Attributes[attributeName];
        return attribute is null ? [] : attribute.GetNotEmptyValues();
    }
}