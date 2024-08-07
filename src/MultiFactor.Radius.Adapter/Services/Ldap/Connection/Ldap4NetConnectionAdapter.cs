//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using LdapForNet;
using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Services.Ldap.Connection.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using LdapForNet.Native;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.ClientLevel;
using MultiFactor.Radius.Adapter.Services.Ldap.Profile;
using static LdapForNet.Native.Native;
using LdapAttributes = MultiFactor.Radius.Adapter.Services.Ldap.Profile.LdapAttributes;

namespace MultiFactor.Radius.Adapter.Services.Ldap.Connection;

public class Ldap4NetConnectionAdapter : ILdapConnectionAdapter
{
    private readonly LdapConnection _connection;
    public string Path { get; }
    private readonly ILogger<Ldap4NetConnectionAdapter> _logger;
    private LdapDomain _whereAmI;

    /// <summary>
    /// Returns user that has been successfully binded with LDAP directory.
    /// </summary>
    public LdapIdentity Username { get; }

    private Ldap4NetConnectionAdapter(string uri, LdapIdentity user, ILogger<Ldap4NetConnectionAdapter> logger)
    {
        _connection = new LdapConnection();
        Path = uri;
        Username = user;
        _logger = logger;
    }

    public async Task<LdapDomain> WhereAmIAsync()
    {
        if (_whereAmI != null) return _whereAmI;

        var filter = "(objectclass=*)";
        var queryResult = await SearchQueryAsync("", filter,  SearchScope.BASE, "defaultNamingContext");
        var result = queryResult.SingleOrDefault() ?? throw new InvalidOperationException($"Unable to query '{Path}' for current user");

        var defaultNamingContext = result.GetValue("defaultNamingContext");
        _whereAmI = LdapDomain.Parse(defaultNamingContext);

        return _whereAmI;
    }

    public async Task<ILdapAttributes[]> SearchQueryAsync(string baseDn, string filter, SearchScope scope, params string[] attributes)
    {
        baseDn ??= string.Empty;
        var sw = Stopwatch.StartNew();
        var searchResult = await _connection.SearchAsync(baseDn, filter, attributes, MapToNative(scope));
        sw.Stop();
        if (sw.Elapsed.TotalSeconds > 2)
        {
            _logger?.LogWarning("Slow response while querying {baseDn:l}. Time elapsed {elapsed}", baseDn, sw.Elapsed);
        }

        if (searchResult.Count == 0)
        {
            return [];
        }

        var collection = new List<ILdapAttributes>();
        foreach (var entry in searchResult)
        {
            var profileAttributes = new LdapAttributes(entry.Dn);
            collection.Add(profileAttributes);
            
            var attrs = entry.DirectoryAttributes;
            var keys = attributes.Where(x => !x.Equals("memberof", StringComparison.OrdinalIgnoreCase));
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

            profileAttributes.Add("memberOf", groups);

            // if (clientConfig.ShouldLoadUserGroups())
            // {
            //     var additionalGroups = await _userGroupsSource.GetUserGroupsAsync(clientConfig, adapter, entry.Dn);
            //     profileAttributes.Add("memberOf", additionalGroups);
            // }
        }
        
        return collection.ToArray();
    }

    private static Native.LdapSearchScope MapToNative(SearchScope scope)
    {
        switch (scope)
        {
            case SearchScope.DEFAULT:
                return LdapSearchScope.LDAP_SCOPE_DEFAULT;
            
            case SearchScope.BASE:
                return LdapSearchScope.LDAP_SCOPE_BASEOBJECT;
            
            case SearchScope.ONELEVEL:
                return LdapSearchScope.LDAP_SCOPE_ONE;
            
            case SearchScope.SUBTREE:
                return LdapSearchScope.LDAP_SCOPE_SUBTREE;
            
            case SearchScope.CHILDREN:
                return LdapSearchScope.LDAP_SCOPE_SUBORDINATE;
            
            default:
                throw new Exception($"Unknown {scope}");
        }
    }

    public static async Task<ILdapConnectionAdapter> CreateAsync(string uri, 
        LdapIdentity user, 
        string password,
        BindIdentityFormatter formatter,
        ILogger<Ldap4NetConnectionAdapter> logger)
    {
        if (uri is null)
        {
            throw new ArgumentNullException(nameof(uri));
        }

        if (user is null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        if (password is null)
        {
            throw new ArgumentNullException(nameof(password));
        }

        if (formatter is null)
        {
            throw new ArgumentNullException(nameof(formatter));
        }

        if (logger is null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        var instance = new Ldap4NetConnectionAdapter(uri, user, logger);

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            instance._connection.TrustAllCertificates();
        }

        if (System.Uri.IsWellFormedUriString(uri, UriKind.Absolute))
        {
            var ldapUri = new Uri(uri);
            instance._connection.Connect(ldapUri.GetLeftPart(UriPartial.Authority));
        }
        else
        {
            instance._connection.Connect(uri, 389);
        }

        instance._connection.SetOption(LdapOption.LDAP_OPT_PROTOCOL_VERSION, (int)LdapVersion.LDAP_VERSION3);
        instance._connection.SetOption(LdapOption.LDAP_OPT_REFERRALS, IntPtr.Zero);

        var bindDn = formatter.FormatIdentity(user, uri);
        await instance._connection.BindAsync(LdapAuthType.Simple, new LdapCredential
        {
            UserName = bindDn,
            Password = password
        });
        return instance;
    }

    public static async Task<ILdapConnectionAdapter> CreateAsTechnicalAccAsync(string domain, 
        IClientConfiguration clientConfig,
        ILogger<Ldap4NetConnectionAdapter> logger)
    {
        try
        {
            var user = LdapIdentity.ParseUser(clientConfig.ServiceAccountUser);
            return await CreateAsync(domain,
                user,
                clientConfig.ServiceAccountPassword,
                new BindIdentityFormatter(clientConfig),
                logger);
        }
        catch (Exception ex)
        {
            throw new TechnicalAccountErrorException(clientConfig.ServiceAccountUser, domain, ex);
        }
    }

    public void Dispose()
    {
        _connection?.Dispose();
        GC.SuppressFinalize(this);
    }
}