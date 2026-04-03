using Multifactor.Core.Ldap.Name;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Abstractions;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Exceptions;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Parser;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Models;

internal sealed class LdapServerConfiguration : ILdapServerConfiguration
{
    public string ConnectionString { get; private init; }
    public string Username { get; private init; }
    public string Password { get; private init; }
    public int BindTimeoutSeconds{ get; private init; }
    public IReadOnlyList<DistinguishedName> AccessGroups { get; private init; }
    public IReadOnlyList<DistinguishedName> SecondFaGroups { get; private init; }
    public IReadOnlyList<DistinguishedName> SecondFaBypassGroups { get; private init; }
    public bool LoadNestedGroups { get; private init; }
    public IReadOnlyList<DistinguishedName> NestedGroupsBaseDns { get; private init; }
    public IReadOnlyList<DistinguishedName> AuthenticationCacheGroups { get; private init; }
    public IReadOnlyList<string> PhoneAttributes { get; private init; }
    public string IdentityAttribute { get; private init; }
    public bool RequiresUpn { get; private init; }
    public bool EnableTrustedDomains { get; private init; }
    public bool AlternativeSuffixesEnabled { get; private init; }
    public IReadOnlyList<string> IncludedDomains { get; init; }
    public IReadOnlyList<string> ExcludedDomains { get; init; }
    public IReadOnlyList<string> IncludedSuffixes { get; init; }
    public IReadOnlyList<string> ExcludedSuffixes { get; init; }
    public IReadOnlyList<string> BypassSecondFactorWhenApiUnreachableGroups { get; init; }

    public static LdapServerConfiguration FromConfiguration(LdapServerSection ldapServerSection, string fileName)
    {
        if (ldapServerSection is { EnableTrustedDomains: true, RequiresUpn: false })
            throw new InvalidConfigurationException($"Config name: '{fileName}', LDAP server: '{ldapServerSection.ConnectionString}'. To use trusted domains also set 'requires-upn' to 'true'.");

        if (!string.IsNullOrWhiteSpace(ldapServerSection.IncludedDomains) && !string.IsNullOrWhiteSpace(ldapServerSection.ExcludedDomains))
            throw new InvalidConfigurationException($"Config name: '{fileName}', LDAP server: '{ldapServerSection.ConnectionString}'. Simultaneous use of 'included-domains' and 'excluded-domains' is not allowed.");

        if (!string.IsNullOrWhiteSpace(ldapServerSection.IncludedSuffixes) && !string.IsNullOrWhiteSpace(ldapServerSection.ExcludedSuffixes))
            throw new InvalidConfigurationException($"Config name: '{fileName}', LDAP server: '{ldapServerSection.ConnectionString}'. Simultaneous use of 'included-suffixes' and 'excluded-suffixes' is not allowed.");
        var dto = new LdapServerConfiguration
        {
            ConnectionString = !string.IsNullOrWhiteSpace(ldapServerSection.ConnectionString) ? ldapServerSection.ConnectionString :
                throw InvalidConfigurationException.For(prop => prop.LdapServers[0].ConnectionString, "Property '{prop}' is required. Config name: '{0}'",  fileName),

            Username = !string.IsNullOrWhiteSpace(ldapServerSection.Username) ? ldapServerSection.Username :
                throw InvalidConfigurationException.For(prop => prop.LdapServers[0].Username, "Property '{prop}' is required. Config name: '{0}'",  fileName),

            Password = !string.IsNullOrWhiteSpace(ldapServerSection.Password) ? ldapServerSection.Password :
                throw InvalidConfigurationException.For(prop => prop.LdapServers[0].Password, "Property '{prop}' is required. Config name: '{0}'",  fileName),

            BindTimeoutSeconds = ldapServerSection.BindTimeoutSeconds ?? 30,
            AccessGroups =
                ConfigurationValueParser.TryParseDistinguishedNames(ldapServerSection.AccessGroups,
                    out var accessGroups)
                    ? accessGroups
                    : [],
            SecondFaGroups =
                ConfigurationValueParser.TryParseDistinguishedNames(ldapServerSection.SecondFaGroups,
                    out var secondFaGroups)
                    ? secondFaGroups
                    : [],
            SecondFaBypassGroups =
                ConfigurationValueParser.TryParseDistinguishedNames(ldapServerSection.SecondFaBypassGroups, out var secondFaBypassGroups)
                    ? secondFaBypassGroups
                    : [],
            LoadNestedGroups = ldapServerSection.LoadNestedGroups,
            NestedGroupsBaseDns =
                ConfigurationValueParser.TryParseDistinguishedNames(ldapServerSection.NestedGroupsBaseDn, out var nestedGroupsBaseDn)
                    ? nestedGroupsBaseDn
                    : [],
            AuthenticationCacheGroups =
                ConfigurationValueParser.TryParseDistinguishedNames(ldapServerSection.AuthenticationCacheGroups, out var authenticationCacheGroups)
                    ? authenticationCacheGroups
                    : [],
            PhoneAttributes =
                ConfigurationValueParser.TryParseStringList(ldapServerSection.PhoneAttributes,
                    out var phoneAttributes)
                    ? phoneAttributes
                    : [],
            IdentityAttribute = ldapServerSection.IdentityAttribute,
            RequiresUpn = ldapServerSection.RequiresUpn,
            EnableTrustedDomains = ldapServerSection.EnableTrustedDomains,
            AlternativeSuffixesEnabled = ldapServerSection.AlternativeSuffixesEnabled,
            IncludedDomains =
                ConfigurationValueParser.TryParseStringList(ldapServerSection.IncludedDomains,
                    out var includedDomains)
                    ? includedDomains
                    : [],
            ExcludedDomains =
                ConfigurationValueParser.TryParseStringList(ldapServerSection.ExcludedDomains,
                    out var excludedDomains)
                    ? excludedDomains
                    : [],
            IncludedSuffixes =
                ConfigurationValueParser.TryParseStringList(ldapServerSection.IncludedSuffixes,
                    out var includedSuffixes)
                    ? includedSuffixes
                    : [],
            ExcludedSuffixes =
                ConfigurationValueParser.TryParseStringList(ldapServerSection.ExcludedSuffixes,
                    out var excludedSuffixes)
                    ? excludedSuffixes
                    : [],
            BypassSecondFactorWhenApiUnreachableGroups = ConfigurationValueParser.TryParseStringList(ldapServerSection.BypassSecondFactorWhenApiUnreachableGroups,
                out var bypassSecondFactorWhenApiUnreachableGroups)
                ? bypassSecondFactorWhenApiUnreachableGroups
                : []

        };
        return dto;
    }
}