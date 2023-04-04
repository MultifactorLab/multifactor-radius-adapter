//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiFactor.Radius.Adapter.Services.Ldap.UserGroupsGetters
{
    public class UserGroupsGetterProvider
    {
        private readonly IEnumerable<IUserGroupsGetter> _getters;

        public UserGroupsGetterProvider(IEnumerable<IUserGroupsGetter> getters)
        {
            _getters = getters ?? throw new ArgumentNullException(nameof(getters));
        }

        public IUserGroupsGetter GetUserGroupsGetter(AuthenticationSource authSource)
        {
            return _getters.FirstOrDefault(x => x.AuthenticationSource.HasFlag(authSource))
                ?? throw new NotImplementedException(authSource.ToString());
        }
    }
}