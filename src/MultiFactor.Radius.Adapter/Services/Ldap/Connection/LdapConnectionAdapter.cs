//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using LdapForNet;
using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Core.Ldap;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static LdapForNet.Native.Native;

namespace MultiFactor.Radius.Adapter.Services.Ldap.Connection;

public class LdapConnectionAdapter : ILdapConnectionAdapter
{
    private readonly LdapConnection _connection;
    public string Uri { get; }
    private readonly LdapConnectionAdapterConfig _config;

    private LdapDomain _whereAmI;

    /// <summary>
    /// Returns user that has been successfully binded with LDAP directory.
    /// </summary>
    public LdapIdentity BindedUser { get; }

    private LdapConnectionAdapter(string uri, LdapIdentity user, LdapConnectionAdapterConfig config)
    {
        _connection = new LdapConnection();
        Uri = uri;
        BindedUser = user;
        _config = config;
    }

    public async Task<LdapDomain> WhereAmIAsync()
    {
        if (_whereAmI != null) return _whereAmI;

        var filter = "(objectclass=*)";
        var queryResult = await SearchQueryAsync("", filter, LdapSearchScope.LDAP_SCOPE_BASEOBJECT, "defaultNamingContext");
        var result = queryResult.SingleOrDefault() ?? throw new InvalidOperationException($"Unable to query '{Uri}' for current user");

        var defaultNamingContext = result.DirectoryAttributes["defaultNamingContext"].GetValue<string>();
        _whereAmI = LdapDomain.Parse(defaultNamingContext);

        return _whereAmI;
    }

    public async Task<LdapEntry[]> SearchQueryAsync(string baseDn, string filter, LdapSearchScope scope, params string[] attributes)
    {
        var sw = Stopwatch.StartNew();
        var searchResult = await _connection.SearchAsync(baseDn, filter, attributes, scope);

        if (sw.Elapsed.TotalSeconds > 2)
        {
            _config.Logger?.LogWarning("Slow response while querying {baseDn:l}. Time elapsed {elapsed}", baseDn, sw.Elapsed);
        }

        return searchResult.ToArray();
    }

    public static async Task<ILdapConnectionAdapter> CreateAsync(string uri, LdapIdentity user, string password,
        Action<LdapConnectionAdapterConfigBuilder> configure = null)
    {
        if (uri is null) throw new ArgumentNullException(nameof(uri));
        if (user is null) throw new ArgumentNullException(nameof(user));
        if (password is null) throw new ArgumentNullException(nameof(password));

        var config = new LdapConnectionAdapterConfig();
        configure?.Invoke(new LdapConnectionAdapterConfigBuilder(config));
        var instance = new LdapConnectionAdapter(uri, user, config);

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

        var bindDn = config.BindIdentityFormatter.FormatIdentity(user, uri);
        await instance._connection.BindAsync(LdapAuthType.Simple, new LdapCredential
        {
            UserName = bindDn,
            Password = password
        });
        return instance;
    }

    public void Dispose()
    {
        _connection?.Dispose();
        GC.SuppressFinalize(this);
    }
}