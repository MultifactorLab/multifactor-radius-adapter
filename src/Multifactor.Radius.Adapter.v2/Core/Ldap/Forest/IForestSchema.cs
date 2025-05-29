using Multifactor.Core.Ldap.Name;

namespace Multifactor.Radius.Adapter.v2.Core.Ldap.Forest;

public interface IForestSchema
{
    DistinguishedName Root { get; }
    public IReadOnlyDictionary<string, DistinguishedName> DomainNameSuffixes { get; }
    DistinguishedName FindDomainByNetbiosName(string netbiosName);
    DistinguishedName? GetMostRelevantDomainBySuffix(string suffix);
}