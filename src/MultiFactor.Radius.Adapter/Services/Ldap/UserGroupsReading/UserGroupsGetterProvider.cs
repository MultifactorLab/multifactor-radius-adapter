//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Infrastructure.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiFactor.Radius.Adapter.Services.Ldap.UserGroupsReading
{
    public class UserGroupsGetterProvider
    {
        private readonly IEnumerable<IUserGroupsGetter> _getters;
        private readonly IUserGroupsGetter _defaultUserGroupsGetter;

        public UserGroupsGetterProvider(IEnumerable<IUserGroupsGetter> getters)
        {
            _getters = getters ?? throw new ArgumentNullException(nameof(getters));
            _defaultUserGroupsGetter = getters.Single(x => x.GetType() == typeof(DefaultUserGroupsGetter));
        }

        public IUserGroupsGetter GetUserGroupsGetter(AuthenticationSource authSource)
        {
            return _getters.FirstOrDefault(x => x.AuthenticationSource.HasFlag(authSource))
                ?? _defaultUserGroupsGetter;
        }
    }
}