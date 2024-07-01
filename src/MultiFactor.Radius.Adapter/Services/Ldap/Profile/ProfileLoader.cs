using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Core.Exceptions;
using MultiFactor.Radius.Adapter.Core.Ldap;
using MultiFactor.Radius.Adapter.Services.Ldap.UserGroupsReading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        var response = await adapter.SearchQueryAsync(domain.Name, searchFilter, LdapSearchScope.LDAP_SCOPE_SUB, queryAttributes);
        var entry = response.SingleOrDefault() ?? throw new LdapUserNotFoundException(user.Name, domain.Name);

        //base profile
        var profileAttributes = new LdapAttributes(entry.Dn);
        var profile = new LdapProfile(LdapIdentity.BaseDn(entry.Dn), profileAttributes, clientConfig.PhoneAttributes, clientConfig.TwoFAIdentityAttribute);

        var attrs = entry.DirectoryAttributes;
        var keys = queryAttributes.Where(x => !x.Equals("memberof", StringComparison.OrdinalIgnoreCase));
        foreach (var key in keys)
        {
            if (!attrs.Contains(key))
            {
                profileAttributes.Add(key, Array.Empty<string>());
                continue;
            }     
            
            var values = attrs[key].GetValues<string>();
            profileAttributes.Add(key, values);    
        }

        //groups
        var groups = attrs.Contains("memberOf")
            ? attrs["memberOf"].GetValues<string>()
            : Array.Empty<string>();

        profileAttributes.Add("memberOf", groups.Select(LdapIdentity.DnToCn).ToArray());

        _logger.LogDebug("User '{User:l}' profile loaded: {DistinguishedName:l} (upn={Upn:l})", 
            user, profile.DistinguishedName, profile.Upn);

        if (clientConfig.ShouldLoadUserGroups())
        {
            var additionalGroups = await _userGroupsSource.GetUserGroupsAsync(clientConfig, adapter, entry.Dn);
            profileAttributes.Add("memberOf", additionalGroups);
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

    public async Task<ILdapAttributes> LoadAttributesAsync(IClientConfiguration clientConfig, ILdapConnectionAdapter adapter, LdapIdentity user, params string[] attrs)
    {
        var names = LdapNames.Create(clientConfig.FirstFactorAuthenticationSource, clientConfig.IsFreeIpa);
        var searchFilter = $"(&(objectClass={names.UserClass})({names.Identity(user)}={user.Name}))";

        var domain = await adapter.WhereAmIAsync();
        _logger.LogDebug("Querying user '{user:l}' in {domainName}", user.Name, domain.Name);

        var response = await adapter.SearchQueryAsync(domain.Name, searchFilter, LdapSearchScope.LDAP_SCOPE_SUB, attrs.Distinct().ToArray());
        var entry = response.SingleOrDefault() ?? throw new LdapUserNotFoundException(user.Name, domain.Name);
        var dirAttrs = entry.DirectoryAttributes;
        var attributes = new LdapAttributes(entry.Dn);
        foreach (var a in attrs)
        {
            if (dirAttrs.TryGetValue(a, out var reqAttr))
            {
                attributes.Add(a, reqAttr.GetValues<string>());
            }
        }

        return attributes;
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
