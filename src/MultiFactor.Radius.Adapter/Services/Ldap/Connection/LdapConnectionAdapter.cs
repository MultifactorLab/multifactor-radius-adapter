//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using LdapForNet;
using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration;
using MultiFactor.Radius.Adapter.Services.Ldap.Connection.Exceptions;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.ClientLevel;
using static LdapForNet.Native.Native;

namespace MultiFactor.Radius.Adapter.Services.Ldap.Connection;

public class LdapConnectionAdapter : ILdapConnectionAdapter
{
    private readonly LdapConnection _connection;
    public string Uri { get; }
    private readonly ILogger<LdapConnectionAdapter> _logger;
    private LdapDomain _whereAmI;

    /// <summary>
    /// Returns user that has been successfully binded with LDAP directory.
    /// </summary>
    public LdapIdentity BindedUser { get; }

    private LdapConnectionAdapter(string uri, LdapIdentity user, ILogger<LdapConnectionAdapter> logger)
    {
        _connection = new LdapConnection();
        Uri = uri;
        BindedUser = user;
        _logger = logger;
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

    public async Task BindAsync(string bindDn, string password)
    {
        if (string.IsNullOrWhiteSpace(bindDn))
        {
            throw new ArgumentNullException(nameof(password));
        }
        
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentNullException(nameof(password));
        }
        
        await _connection.BindAsync(LdapAuthType.Simple, new LdapCredential
        {
            UserName = bindDn,
            Password = password
        });
    }

    public async Task<LdapEntry[]> SearchQueryAsync(string baseDn, string filter, LdapSearchScope scope, params string[] attributes)
    {
        var sw = Stopwatch.StartNew();
        var searchResult = await _connection.SearchAsync(baseDn, filter, attributes, scope);

        if (sw.Elapsed.TotalSeconds > 2)
        {
            _logger?.LogWarning("Slow response while querying {baseDn:l}. Time elapsed {elapsed}", baseDn, sw.Elapsed);
        }

        return searchResult.ToArray();
    }

    public static ILdapConnectionAdapter CreateAsync(
        string uri, 
        LdapIdentity user,
        ILogger<LdapConnectionAdapter> logger,
        TimeSpan? timeout = null)
    {
        if (uri is null)
        {
            throw new ArgumentNullException(nameof(uri));
        }

        if (user is null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        if (logger is null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        var instance = new LdapConnectionAdapter(uri, user, logger);

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
        if (timeout.HasValue)
        {
            instance._connection.Timeout = timeout.Value;
        }
        
        return instance;
    }

    public static ILdapConnectionAdapter CreateAsTechnicalAccAsync(
        string domain, 
        IClientConfiguration clientConfig,
        ILogger<LdapConnectionAdapter> logger)
    {
        try
        {
            var user = LdapIdentity.ParseUser(clientConfig.ServiceAccountUser);
            return CreateAsync(domain, user, logger);
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