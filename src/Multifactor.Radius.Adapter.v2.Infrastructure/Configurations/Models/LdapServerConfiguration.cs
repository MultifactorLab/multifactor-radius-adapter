using Multifactor.Core.Ldap.Name;
using Multifactor.Radius.Adapter.v2.Application.Configuration.Models;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Exceptions;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Parser;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Models;

public class LdapServerConfiguration : ILdapServerConfiguration
{
    public string ConnectionString { get; init; }
    public string Username { get; init; }
    public string Password { get; init; }
    public int BindTimeoutSeconds{ get; init; }
    public IReadOnlyList<DistinguishedName> AccessGroups { get; init; }
    public IReadOnlyList<DistinguishedName> SecondFaGroups { get; init; }
    public IReadOnlyList<DistinguishedName> SecondFaBypassGroups { get; init; }
    public bool LoadNestedGroups { get; init; }
    public IReadOnlyList<DistinguishedName> NestedGroupsBaseDns { get; init; }
    public IReadOnlyList<DistinguishedName> AuthenticationCacheGroups { get; init; }
    public IReadOnlyList<string> PhoneAttributes { get; init; }
    public string IdentityAttribute { get; init; }
    public bool RequiresUpn { get; init; }
    public bool TrustedDomainsEnabled { get; init; }
    public bool AlternativeSuffixesEnabled { get; init; }
    public IReadOnlyList<string> IncludedDomains { get; init; }//TODO not used
    public IReadOnlyList<string> ExcludedDomains { get; init; }//TODO not used
    public IReadOnlyList<string> IncludedSuffixes { get; init; }
    public IReadOnlyList<string> ExcludedSuffixes { get; init; }
    public IReadOnlyList<string> BypassSecondFactorWhenApiUnreachableGroups { get; init; }

    public static LdapServerConfiguration FromConfiguration(LdapServerSection ldapServerSection)
    {
        var dto = new LdapServerConfiguration
        {
            ConnectionString = !string.IsNullOrWhiteSpace(ldapServerSection.ConnectionString) ? ldapServerSection.ConnectionString :
                throw InvalidConfigurationException.RequiredFor(section => section.LdapServers[0].ConnectionString, nameof(ldapServerSection)),
            Username = !string.IsNullOrWhiteSpace(ldapServerSection.Username) ? ldapServerSection.Username :
                throw InvalidConfigurationException.RequiredFor(section => section.LdapServers[0].Username, nameof(ldapServerSection)),
            Password = !string.IsNullOrWhiteSpace(ldapServerSection.Password) ? ldapServerSection.Password :
                throw InvalidConfigurationException.RequiredFor(section => section.LdapServers[0].Password, nameof(ldapServerSection)),
            BindTimeoutSeconds = ldapServerSection.BindTimeoutSeconds ?? 30,
            AccessGroups =
                ConfigurationValueProcessor.TryParseDistinguishedNames(ldapServerSection.AccessGroups,
                    out var accessGroups)
                    ? accessGroups
                    : [],
            SecondFaGroups =
                ConfigurationValueProcessor.TryParseDistinguishedNames(ldapServerSection.SecondFaGroups,
                    out var secondFaGroups)
                    ? secondFaGroups
                    : [],
            SecondFaBypassGroups =
                ConfigurationValueProcessor.TryParseDistinguishedNames(ldapServerSection.SecondFaBypassGroups, out var secondFaBypassGroups)
                    ? secondFaBypassGroups
                    : [],
            LoadNestedGroups =ldapServerSection.LoadNestedGroups,
            NestedGroupsBaseDns =
                ConfigurationValueProcessor.TryParseDistinguishedNames(ldapServerSection.NestedGroupsBaseDns, out var nestedGroupsBaseDns)
                    ? nestedGroupsBaseDns
                    : [],
            AuthenticationCacheGroups =
                ConfigurationValueProcessor.TryParseDistinguishedNames(ldapServerSection.AuthenticationCacheGroups, out var authenticationCacheGroups)
                    ? authenticationCacheGroups
                    : [],
            PhoneAttributes =
                ConfigurationValueProcessor.TryParseStringList(ldapServerSection.PhoneAttributes,
                    out var phoneAttributes)
                    ? phoneAttributes
                    : [],
            IdentityAttribute = ldapServerSection.IdentityAttribute ?? "sAMAccountName",
            RequiresUpn = ldapServerSection.RequiresUpn,
            TrustedDomainsEnabled = ldapServerSection.TrustedDomainsEnabled,
            AlternativeSuffixesEnabled =ldapServerSection.AlternativeSuffixesEnabled,
            IncludedDomains =
                ConfigurationValueProcessor.TryParseStringList(ldapServerSection.IncludedDomains,
                    out var includedDomains)
                    ? includedDomains
                    : [],
            ExcludedDomains =
                ConfigurationValueProcessor.TryParseStringList(ldapServerSection.ExcludedDomains,
                    out var excludedDomains)
                    ? excludedDomains
                    : [],
            IncludedSuffixes =
                ConfigurationValueProcessor.TryParseStringList(ldapServerSection.IncludedSuffixes,
                    out var includedSuffixes)
                    ? includedSuffixes
                    : [],
            ExcludedSuffixes =
                ConfigurationValueProcessor.TryParseStringList(ldapServerSection.ExcludedSuffixes,
                    out var excludedSuffixes)
                    ? excludedSuffixes
                    : [],
            BypassSecondFactorWhenApiUnreachableGroups = ConfigurationValueProcessor.TryParseStringList(ldapServerSection.BypassSecondFactorWhenApiUnreachableGroups,
                out var bypassSecondFactorWhenApiUnreachableGroups)
                ? bypassSecondFactorWhenApiUnreachableGroups
                : []
        };
        return dto;
    }
}