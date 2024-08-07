using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Core.Exceptions;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration;
using MultiFactor.Radius.Adapter.Services.Ldap.Connection;
using MultiFactor.Radius.Adapter.Services.Ldap.UserGroupsReading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.ClientLevel;
using static LdapForNet.Native.Native;

namespace MultiFactor.Radius.Adapter.Services.Ldap.Profile;

public class ProfileLoader
{
    private readonly UserGroupsSource _userGroupsSource;
    private readonly ILogger _logger;

    public ProfileLoader(UserGroupsSource userGroupsSource, ILogger<ProfileLoader> logger)
    {
        _userGroupsSource = userGroupsSource ?? throw new ArgumentNullException(nameof(userGroupsSource));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<LdapProfile> LoadAsync(IClientConfiguration clientConfig, ILdapConnectionAdapter adapter, LdapIdentity user)
    {
        var queryAttributes = GetQueryAttributes(clientConfig);

        var names = LdapNames.Create(clientConfig.FirstFactorAuthenticationSource, clientConfig.IsFreeIpa);
        var searchFilter = $"(&(objectClass={names.UserClass})({names.Identity(user)}={user.Name}))";

        var domain = await adapter.WhereAmIAsync();
        _logger.LogDebug("Querying user '{user:l}' in {domainName:l}", user.Name, domain.Name);

        var response = await adapter.SearchQueryAsync(domain.Name, searchFilter, SearchScope.SUBTREE, queryAttributes);
        var entry = response.SingleOrDefault() ?? throw new LdapUserNotFoundException(user.Name, domain.Name);

        //base profile
        var profile = new LdapProfile(LdapIdentity.BaseDn(entry.DistinguishedName), entry, clientConfig.PhoneAttributes, clientConfig.TwoFAIdentityAttribute);

        _logger.LogDebug("User '{User:l}' profile loaded: {DistinguishedName:l} (upn={Upn:l})", 
            user, profile.DistinguishedName, profile.Upn);

        if (clientConfig.ShouldLoadUserGroups())
        {
            var additionalGroups = await _userGroupsSource.GetUserGroupsAsync(clientConfig, adapter, entry.DistinguishedName); 
            // profileAttributes.Add("memberOf", additionalGroups);
        }

        return profile;
    }

    private static string[] GetQueryAttributes(IClientConfiguration clientConfig)
    {
        var queryAttributes = new List<string> { "DistinguishedName", "displayName", "mail", "memberOf", "userPrincipalName" };
        if (clientConfig.UseIdentityAttribute)
        {
            queryAttributes.Add(clientConfig.TwoFAIdentityAttribute);
        }

        //additional attributes for radius response
        queryAttributes.AddRange(GetLdapReplyAttributes(clientConfig));
        queryAttributes.AddRange(clientConfig.PhoneAttributes);

        return queryAttributes.Distinct().ToArray();
    }

    public async Task<ILdapAttributes> LoadAttributesAsync(IClientConfiguration clientConfig, ILdapConnectionAdapter adapter, 
        LdapIdentity user, params string[] attrs)
    {
        var names = LdapNames.Create(clientConfig.FirstFactorAuthenticationSource, clientConfig.IsFreeIpa);
        var searchFilter = $"(&(objectClass={names.UserClass})({names.Identity(user)}={user.Name}))";

        var domain = await adapter.WhereAmIAsync();
        _logger.LogDebug("Querying user '{user:l}' in {domainName}", user.Name, domain.Name);

        var response = await adapter.SearchQueryAsync(domain.Name, searchFilter, SearchScope.SUBTREE, attrs.Distinct().ToArray());
        var entry = response.SingleOrDefault() ?? throw new LdapUserNotFoundException(user.Name, domain.Name);

        return entry;
    }

    private static string[] GetLdapReplyAttributes(IClientConfiguration config)
    {
        return config.RadiusReplyAttributes
            .Values
            .SelectMany(attr => attr)
            .Where(attr => attr.FromLdap)
            .Select(attr => attr.LdapAttributeName)
            .ToArray();
    }
}
