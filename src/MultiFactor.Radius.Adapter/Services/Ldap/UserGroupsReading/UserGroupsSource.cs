//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Services.Ldap.Connection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.ClientLevel;

namespace MultiFactor.Radius.Adapter.Services.Ldap.UserGroupsReading
{
    public class UserGroupsSource
    {
        private readonly UserGroupsGetterProvider _provider;
        private readonly ILogger _logger;

        public UserGroupsSource(UserGroupsGetterProvider provider, ILogger<UserGroupsGetterProvider> logger)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string[]> GetUserGroupsAsync(IClientConfiguration clientConfig, ILdapConnectionAdapter connectionAdapter, string userDn)
        {
            var getter = _provider.GetUserGroupsGetter(clientConfig.FirstFactorAuthenticationSource);
            var allUserGroupsNames = new List<string>();
            var ldapDomain = await connectionAdapter.WhereAmIAsync();
            var baseDnsForSearch = clientConfig.SplittedNestedGroupsBaseDn?.Length > 0 ? clientConfig.SplittedNestedGroupsBaseDn : new string[1] { ldapDomain.Name };
            foreach (var baseDn in baseDnsForSearch)
            {
                var sw = Stopwatch.StartNew();
                var foundGroupsNames = await getter.GetUserGroupsFromDnAsync(connectionAdapter, baseDn, userDn, clientConfig.LoadActiveDirectoryNestedGroups);
                sw.Stop();
                _logger.LogDebug("Search in {baseDn} for user {userDn} took {ms}ms", baseDn, userDn, sw.ElapsedMilliseconds);
                if (foundGroupsNames.Length > 0)
                {
                    allUserGroupsNames.AddRange(foundGroupsNames);
                    _logger.LogDebug("Found groups in {baseDn}: {groups}", baseDn, string.Join(",", foundGroupsNames.Select(x => $"'{x}'")));
                }
                else
                {
                    _logger.LogWarning("User '{dn:l}' does not have any group in {baseDn}.", userDn, baseDn);
                }
            }

            return allUserGroupsNames.ToArray(); 
        }
    }
}