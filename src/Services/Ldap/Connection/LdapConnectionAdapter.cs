//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using LdapForNet;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static LdapForNet.Native.Native;

namespace MultiFactor.Radius.Adapter.Services.Ldap.Connection
{
    public class LdapConnectionAdapter : IDisposable
    {
        private readonly LdapConnection _connection;
        public string Uri { get; }
        private readonly LdapConnectionAdapterConfig _config;

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
            var filter = "(objectclass=*)";
            var queryResult = await SearchQueryAsync("", filter, LdapSearchScope.LDAP_SCOPE_BASEOBJECT, "defaultNamingContext");
            var result = queryResult.SingleOrDefault() ?? throw new InvalidOperationException($"Unable to query {Uri} for current user");

            var defaultNamingContext = result.DirectoryAttributes["defaultNamingContext"].GetValue<string>();
            return LdapDomain.Parse(defaultNamingContext);
        }

        public async Task<IList<LdapEntry>> SearchQueryAsync(string baseDn, string filter, LdapSearchScope scope, params string[] attributes)
        {
            if (_config.Logger == null)
            {
                return await _connection.SearchAsync(baseDn, filter, attributes, scope);
            }

            var sw = Stopwatch.StartNew();
            var searchResult = await _connection.SearchAsync(baseDn, filter, attributes, scope);

            if (sw.Elapsed.TotalSeconds > 2)
            {
                _config.Logger.Warning("Slow response while querying {baseDn:l}. Time elapsed {elapsed}", baseDn, sw.Elapsed);
            }

            return searchResult;
        }

        public Task<DirectoryResponse> SendRequestAsync(DirectoryRequest request)
        {
            return _connection.SendRequestAsync(request);
        }

        public static async Task<LdapConnectionAdapter> CreateAsync(string uri,
            Action<LdapConnectionAdapterConfigBuilder> configure = null)
        {
            if (uri is null) throw new ArgumentNullException(nameof(uri));

            var config = new LdapConnectionAdapterConfig();
            configure?.Invoke(new LdapConnectionAdapterConfigBuilder(config));
            var instance = new LdapConnectionAdapter(uri, null, config);

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

            await instance._connection.BindAsync(LdapAuthType.Simple, new LdapCredential
            {
                // TODO: remove it before commit
                UserName = "ssp.service.user@multifactor.local",
                Password = "Qwerty123!"
            });
            return instance;
        }

        public static async Task<LdapConnectionAdapter> CreateAsync(string uri, LdapIdentity user, string password,
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
}